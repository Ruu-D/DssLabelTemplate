using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace DssLabelTemplate;

/// <summary>
/// The right column: the preview of ONE label (<see cref="Index"/>) of the <see cref="LabelSet"/>, as it would be printed,
/// with below it where it sits (ID, X, Y, rotation) and a small map of the sheet with all its labels.
/// Top to bottom: the label (as big as fits), an info line, a line for problems, the sheet map.
/// </summary>
internal class LabelPreview : Control
{
    private const int Margin16 = 16;   // room around everything, at 100%
    private const int Gap = 8;         // between the parts, at 100%
    private const float SheetMapShare = 0.25f; // the sheet map gets this part of the height

    private readonly LabelImages _images = new();
    private LabelSet? _labels;
    private string? _baseFolder;
    private int _index;
    private SizeF _labelSize = new((float)AppSettings.DefaultLabelSize, (float)AppSettings.DefaultLabelSize);
    private int _sheetRotation;

    public LabelPreview()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false; // nothing to type here
    }

    /// <summary>What is shown now (null = nothing opened).</summary>
    internal LabelSet? Labels => _labels;

    /// <summary>The number of labels that can be shown.</summary>
    internal int Count => _labels?.Labels.Count ?? 0;

    /// <summary>The problems found while drawing the label on screen (print objects that could not be drawn).</summary>
    internal List<string> Problems { get; } = new();

    /// <summary>How many print objects of the shown label lie (partly) outside the label (found while drawing).</summary>
    internal int ObjectsOutside { get; private set; }

    /// <summary>The size of the label in mm (length along X, width along Y): the two fields in the window.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public SizeF LabelSize
    {
        get => _labelSize;
        set
        {
            _labelSize = value;
            Invalidate();
        }
    }

    /// <summary>
    /// How far the sheet map is turned, in degrees counter-clockwise (<see cref="AppSettings.SheetRotationInViewer"/>):
    /// 0, 90, 180, 270 or the negative ones. Only the sheet map turns; the label itself stays upright (as printed).
    /// </summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SheetRotation
    {
        get => _sheetRotation;
        set
        {
            _sheetRotation = value;
            Invalidate();
        }
    }

    /// <summary>The label that is shown (0 = the first). Always within 0 .. Count - 1.</summary>
    [System.ComponentModel.Browsable(false)] // these two attributes tell the Visual Studio designer to ignore it
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Index
    {
        get => _index;
        set
        {
            _index = Math.Max(0, Math.Min(value, Count - 1));
            Invalidate();
        }
    }

    /// <param name="baseFolder">The folder of the label file: an Image may have a path relative to it.</param>
    /// <param name="keepIndex">True while typing (stay at the same label); false for a newly opened file (the first label).</param>
    public void ShowLabels(LabelSet? labels, string? baseFolder, bool keepIndex)
    {
        _labels = labels;
        _baseFolder = baseFolder;
        Index = keepIndex ? _index : 0;
    }

    /// <summary>Forget the pictures that were read: another file was opened.</summary>
    public void ForgetImages() => _images.Clear();

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(FluentDrawing.BehindColor(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Problems.Clear();
        ObjectsOutside = 0;

        // The same rounded frame as the editor next to it, on the very pale blue of the panels.
        float radius = LogicalToDeviceUnits(FluentDrawing.CornerRadius);
        using (GraphicsPath frame = FluentDrawing.RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), radius))
        using (var fill = new SolidBrush(Theme.VeryPaleBlue))
        using (var pen = new Pen(Theme.LightBlue, 1))
        {
            g.FillPath(fill, frame);
            g.DrawPath(pen, frame);
        }

        int margin = LogicalToDeviceUnits(Margin16);
        var inside = new Rectangle(margin, margin, Math.Max(0, Width - 2 * margin), Math.Max(0, Height - 2 * margin));

        if (_labels == null)
        {
            DrawMessage(g, inside, Strings.PreviewNoFile, Theme.Navy);
        }
        else if (_labels.Error != null)
        {
            DrawMessage(g, inside, _labels.Error, Theme.Error);
        }
        else if (Count == 0)
        {
            DrawMessage(g, inside, _labels.Message ?? Strings.PreviewEmptyLabelFile, Theme.Navy);
        }
        else
        {
            DrawLabelAndSheet(g, inside);
        }
    }

    private void DrawLabelAndSheet(Graphics g, Rectangle inside)
    {
        LabelItem label = _labels!.Labels[_index];
        int gap = LogicalToDeviceUnits(Gap);
        int line = Font.Height;
        bool hasSheet = label.SheetLength > 0 && label.SheetWidth > 0;
        int mapHeight = hasSheet ? (int)(inside.Height * SheetMapShare) : 0;
        int labelHeight = inside.Height - 2 * line - gap - (hasSheet ? mapHeight + gap : 0);
        if (labelHeight <= 0)
        {
            return; // the window is too small to show anything sensible
        }

        // 1. The label (the size of the two fields in the window), as big as fits, in the middle. Objects that stick out
        //    of the label are drawn too, on the pale blue around it: the view is made big enough for them.
        SizeF size = _labelSize;
        List<RectangleF> objects = LabelRenderer.ObjectBounds(label);
        ObjectsOutside = LabelRenderer.CountOutside(objects, size);
        RectangleF view = LabelRenderer.ViewArea(objects, size);
        float scale = Math.Min(inside.Width / view.Width, labelHeight / view.Height);
        float viewLeft = inside.Left + (inside.Width - view.Width * scale) / 2;
        float viewTop = inside.Top + (labelHeight - view.Height * scale) / 2;
        var paper = new RectangleF(viewLeft - view.X * scale, viewTop - view.Y * scale, size.Width * scale, size.Height * scale);

        using (var white = new SolidBrush(Theme.LabelPaper))
        using (var border = new Pen(Theme.LightBlue, 1))
        {
            g.FillRectangle(white, paper);
            g.DrawRectangle(border, paper.X, paper.Y, paper.Width, paper.Height);
        }

        GraphicsState state = g.Save();
        g.TextRenderingHint = TextRenderingHint.AntiAlias;
        g.TranslateTransform(paper.X, paper.Y);
        g.ScaleTransform(scale, scale); // from here on, one unit is one millimetre on the label
        LabelRenderer.Draw(g, label, _baseFolder, _images, Problems);
        g.Restore(state);

        // 2. Where the label sits on the sheet, and 3. what could not be drawn.
        int top = inside.Top + labelHeight + gap;
        string info = Strings.LabelInfo(label.Id, NumberText(label.X), NumberText(label.Y), label.Rotation.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (hasSheet)
        {
            info += "  ·  " + Strings.SheetInfo(LabelXml.Format(label.SheetLength!.Value), LabelXml.Format(label.SheetWidth!.Value));
        }

        DrawLine(g, new Rectangle(inside.Left, top, inside.Width, line), info, Theme.Navy);
        var problemLine = new Rectangle(inside.Left, top + line, inside.Width, line);
        if (label.MissingVariables.Count > 0)
        {
            // First: it is usually the cause of the other problems (a "?" above, an object that cannot be drawn).
            string names = string.Join(", ", label.MissingVariables.Select(name => "$" + name + "$"));
            DrawLine(g, problemLine, Strings.VariablesNotInCsv(names), Theme.Error);
        }
        else if (Problems.Count > 0)
        {
            DrawLine(g, problemLine, Strings.ObjectsNotDrawn(Problems.Count, Problems[0]), Theme.Error);
        }
        else if (ObjectsOutside > 0)
        {
            DrawLine(g, problemLine, Strings.ObjectsOutsideLabel(ObjectsOutside), Theme.Error);
        }

        // 4. The sheet with all its labels.
        if (hasSheet)
        {
            DrawSheetMap(g, new Rectangle(inside.Left, top + 2 * line + gap, inside.Width, mapHeight), label, size);
        }
    }

    /// <summary>
    /// The sheet seen from above, with a small square for every label and the shown label in the brand colour. X along the
    /// length, Y along the width, from the bottom left corner (TopSolid's corner; the real reference corner is a setting in
    /// DSSClient). X, Y is the middle of the label (Duivestein's description of the label-csv).
    /// The whole map (sheet and labels) is turned by <see cref="SheetRotation"/>, so it matches how the operator sees the
    /// sheet from the label PC.
    /// </summary>
    private void DrawSheetMap(Graphics g, Rectangle area, LabelItem shown, SizeF labelSize)
    {
        float length = (float)shown.SheetLength!.Value, width = (float)shown.SheetWidth!.Value;
        int turns = QuarterTurns(_sheetRotation);
        float scale = MapScale(area, length, width, turns);

        // Two opposite corners of the sheet, wherever the turn puts them.
        PointF corner1 = MapPoint(area, length, width, turns, 0, 0);
        PointF corner2 = MapPoint(area, length, width, turns, length, width);
        RectangleF sheet = RectangleF.FromLTRB(Math.Min(corner1.X, corner2.X), Math.Min(corner1.Y, corner2.Y),
            Math.Max(corner1.X, corner2.X), Math.Max(corner1.Y, corner2.Y));

        using (var white = new SolidBrush(Theme.White))
        using (var border = new Pen(Theme.LightBlue, 1))
        {
            g.FillRectangle(white, sheet);
            g.DrawRectangle(border, sheet.X, sheet.Y, sheet.Width, sheet.Height);
        }

        // A quarter turn also turns the labels: their length then runs up and down the map.
        float labelAcross = turns % 2 == 0 ? labelSize.Width : labelSize.Height;
        float labelUpDown = turns % 2 == 0 ? labelSize.Height : labelSize.Width;
        float minimum = LogicalToDeviceUnits(4);
        float squareWidth = Math.Max(minimum, labelAcross * scale), squareHeight = Math.Max(minimum, labelUpDown * scale);
        using var others = new SolidBrush(Theme.PaleBlue);
        using var current = new SolidBrush(Theme.Brand);
        using var outline = new Pen(Theme.Navy, 1);

        for (int i = 0; i < Count; i++)
        {
            LabelItem label = _labels!.Labels[i];
            if (label.X is not double x || label.Y is not double y || i == _index)
            {
                continue;
            }

            g.FillRectangle(others, Square(MapPoint(area, length, width, turns, x, y), squareWidth, squareHeight));
        }

        if (shown.X is double shownX && shown.Y is double shownY)
        {
            RectangleF square = Square(MapPoint(area, length, width, turns, shownX, shownY), squareWidth, squareHeight);
            g.FillRectangle(current, square);
            g.DrawRectangle(outline, square.X, square.Y, square.Width, square.Height);
        }
    }

    private static RectangleF Square(PointF centre, float width, float height) =>
        new(centre.X - width / 2, centre.Y - height / 2, width, height);

    /// <summary>The number of quarter turns counter-clockwise (0 – 3) for a rotation in degrees: -90 = 3, 180 = 2, 360 = 0.</summary>
    internal static int QuarterTurns(int degrees) => ((degrees / 90) % 4 + 4) % 4;

    /// <summary>Screen pixels per mm of the sheet: as big as fits in the area, with the sheet turned by <paramref name="turns"/>.</summary>
    internal static float MapScale(RectangleF area, float length, float width, int turns) =>
        turns % 2 == 0
            ? Math.Min(area.Width / length, area.Height / width)
            : Math.Min(area.Width / width, area.Height / length); // a quarter turn: the length runs up and down

    /// <summary>
    /// Where a point of the sheet (mm, X along the length, Y upwards from the bottom left corner) lands in the map: the
    /// sheet in the middle of the area, as big as fits, turned <paramref name="turns"/> quarter turns counter-clockwise.
    /// </summary>
    internal static PointF MapPoint(RectangleF area, float length, float width, int turns, double x, double y)
    {
        float scale = MapScale(area, length, width, turns);

        // From the middle of the sheet, Y still upwards.
        float dx = (float)x - length / 2, dy = (float)y - width / 2;

        // Turn counter-clockwise, a quarter at a time: (dx, dy) becomes (-dy, dx).
        for (int i = 0; i < turns; i++)
        {
            (dx, dy) = (-dy, dx);
        }

        float middleX = area.Left + area.Width / 2, middleY = area.Top + area.Height / 2;
        return new PointF(middleX + dx * scale, middleY - dy * scale); // on the screen Y counts downwards
    }

    private void DrawMessage(Graphics g, Rectangle area, string text, Color color)
    {
        TextRenderer.DrawText(g, text, Font, area, color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
    }

    private void DrawLine(Graphics g, Rectangle area, string text, Color color)
    {
        TextRenderer.DrawText(g, text, Font, area, color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private static string NumberText(double? value) => value is double number ? LabelXml.Format(number) : "?";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _images.Dispose();
        }

        base.Dispose(disposing);
    }
}
