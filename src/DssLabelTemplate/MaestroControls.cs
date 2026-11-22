using System.Drawing.Drawing2D;

namespace DssLabelTemplate;

// The controls of the "Maestro" skin (2.0.0, Daan 2026-11-21): the customer works all day with the SCM machine software
// (Maestro) and asked for the same look. Like FluentControls.cs these controls paint themselves, and are still a normal
// Button / Control for the rest of the program (Text, Click, Visible, PerformClick, ...). All colours come from Theme,
// every size is written for 96 dpi and scaled with LogicalToDeviceUnits.

/// <summary>Drawing helpers shared by the controls below.</summary>
internal static class MaestroDrawing
{
    /// <summary>
    /// A round disc that is lighter at the top left, like the round buttons of Maestro: <paramref name="light"/> at that
    /// point, <paramref name="middle"/> about halfway and <paramref name="edge"/> at the rim.
    /// </summary>
    public static void FillDisc(Graphics g, RectangleF circle, Color light, Color middle, Color edge)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(circle);
        using var brush = new PathGradientBrush(path)
        {
            CenterPoint = new PointF(circle.Left + circle.Width * 0.35f, circle.Top + circle.Height * 0.30f),
            // Positions run from the rim (0) to the centre point (1).
            InterpolationColors = new ColorBlend { Colors = new[] { edge, middle, light }, Positions = new[] { 0f, 0.45f, 1f } },
        };
        g.FillPath(brush, path);
    }

    /// <summary>A rectangle whose two short ends are half circles (the print button, the end of the bottom bar).</summary>
    public static GraphicsPath Pill(RectangleF rect)
    {
        float diameter = Math.Min(rect.Width, rect.Height);
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 90, 180);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 180);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// A cog wheel (the Magic parameters, 2.0.1, the same as TsDssConverter's): eight flat teeth around a wheel, with a
    /// round hole in the middle in <paramref name="hole"/> (the colour behind the cog).
    /// </summary>
    public static void DrawCog(Graphics g, RectangleF box, Color ink, Color hole)
    {
        float centreX = box.Left + box.Width / 2;
        float centreY = box.Top + box.Height / 2;
        float outer = Math.Min(box.Width, box.Height) / 2;
        float inner = outer * 0.74f;

        // Every tooth: from the wheel up to the tip and down again, a little narrower at the tip than at its foot.
        var points = new List<PointF>();
        for (int tooth = 0; tooth < 8; tooth++)
        {
            double middle = tooth * Math.PI / 4;
            foreach (var (turn, radius) in new[] { (-0.30, inner), (-0.17, outer), (0.17, outer), (0.30, inner) })
            {
                points.Add(new PointF(centreX + radius * (float)Math.Cos(middle + turn), centreY + radius * (float)Math.Sin(middle + turn)));
            }
        }

        using (var brush = new SolidBrush(ink))
        {
            g.FillPolygon(brush, points.ToArray());
        }

        float hollow = outer * 0.36f;
        using var holeBrush = new SolidBrush(hole);
        g.FillEllipse(holeBrush, centreX - hollow, centreY - hollow, 2 * hollow, 2 * hollow);
    }

    /// <summary>The tick of Save (on a grid of 24 in <paramref name="box"/>): from the left, down, then up to the right.</summary>
    public static void DrawTick(Graphics g, RectangleF box, Color colour)
    {
        float unit = box.Width / 24;
        PointF P(float x, float y) => new(box.Left + x * unit, box.Top + y * unit);
        using var pen = new Pen(colour, 2.8f * unit) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(pen, new[] { P(5, 12.5f), P(9.5f, 17), P(19, 7.5f) });
    }

    /// <summary>A soft shadow below a shape: the same shape a little lower, in see-through black.</summary>
    public static void FillShadow(Graphics g, GraphicsPath shape, float down, int alpha)
    {
        using var shadow = (GraphicsPath)shape.Clone();
        using var move = new Matrix();
        move.Translate(0, down);
        shadow.Transform(move);
        using var brush = new SolidBrush(Color.FromArgb(alpha, Color.Black));
        g.FillPath(brush, shadow);
    }

    /// <summary>
    /// The board sign of the mockup (Daan, 2026-11-21: "use the icons from the Claude mockup"): a sheet with a few parts
    /// on it, as lines, on a grid of 24 in <paramref name="box"/>.
    /// </summary>
    public static void DrawBoardIcon(Graphics g, RectangleF box, Color colour)
    {
        float unit = box.Width / 24;
        using var pen = new Pen(colour, 2 * unit) { LineJoin = LineJoin.Miter };
        PointF P(float x, float y) => new(box.Left + x * unit, box.Top + y * unit);
        g.DrawRectangle(pen, box.Left + 2.5f * unit, box.Top + 4.5f * unit, 19 * unit, 15 * unit);
        g.DrawLine(pen, P(2.5f, 11), P(11.5f, 11));
        g.DrawLine(pen, P(11.5f, 4.5f), P(11.5f, 19.5f));
        g.DrawLine(pen, P(11.5f, 14), P(21.5f, 14));
        g.DrawLine(pen, P(16, 14), P(16, 19.5f));
    }

    /// <summary>The printer sign of the mockup: the paper tray at the top, the body, the printed sheet; lines on a grid of 24.</summary>
    public static void DrawPrinterIcon(Graphics g, RectangleF box, Color colour)
    {
        float unit = box.Width / 24;
        using var pen = new Pen(colour, 2 * unit) { LineJoin = LineJoin.Round };
        PointF P(float x, float y) => new(box.Left + x * unit, box.Top + y * unit);
        g.DrawLines(pen, new[] { P(7, 9), P(7, 3), P(17, 3), P(17, 9) });
        using (GraphicsPath body = FluentDrawing.RoundedRectangle(new RectangleF(P(3, 9), new SizeF(18 * unit, 8 * unit)), unit))
        {
            g.DrawPath(pen, body);
        }

        g.DrawRectangle(pen, box.Left + 7 * unit, box.Top + 14 * unit, 10 * unit, 7 * unit);
    }

    /// <summary>A picture in the middle of <paramref name="box"/>, <paramref name="height"/> high with its own proportions; pale when disabled.</summary>
    public static void DrawPicture(Graphics g, Image picture, RectangleF box, float height, bool enabled)
    {
        float width = height * picture.Width / picture.Height;
        if (width > box.Width)
        {
            height *= box.Width / width;
            width = box.Width;
        }

        var place = new RectangleF(box.Left + (box.Width - width) / 2, box.Top + (box.Height - height) / 2, width, height);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        if (enabled)
        {
            g.DrawImage(picture, place);
        }
        else
        {
            using var pale = new System.Drawing.Imaging.ImageAttributes();
            pale.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.3f });
            g.DrawImage(picture, Rectangle.Round(place), 0, 0, picture.Width, picture.Height, GraphicsUnit.Pixel, pale);
        }

        g.PixelOffsetMode = PixelOffsetMode.Default;
    }
}

/// <summary>
/// A control that paints a background its round children must show around their shape (the bottom bar's gradient).
/// </summary>
internal interface IPaintsBackgroundForChildren
{
    /// <summary>Paints this control's background into <paramref name="g"/>, which is the graphics of <paramref name="child"/>.</summary>
    void PaintBackgroundFor(Control child, Graphics g);
}

/// <summary>
/// A button with a round shape that shows the background of its parent around it (the gradient of the bottom bar). It
/// asks the parent to paint that background into the button itself (<see cref="IPaintsBackgroundForChildren"/>), every
/// time it is painted. (2.0.0-alpha used BackColor = Transparent: on Daan's screen that left grey and white squares
/// around the round buttons, also while the mouse was over them; a rendered picture did not show it.) Tracks the mouse
/// for the hover and pressed look.
/// </summary>
internal abstract class ShapedButton : Button
{
    protected bool Hover;
    protected bool Pressed;

    protected ShapedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    /// <summary>First the parent's background (the band), then the button: no square around the round shape.</summary>
    protected void PaintParentBackground(Graphics g)
    {
        if (Parent is IPaintsBackgroundForChildren painter)
        {
            painter.PaintBackgroundFor(this, g);
        }
        else
        {
            g.Clear(Parent?.BackColor ?? Theme.White);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Nothing here: OnPaint paints the parent's background itself (Opaque: Windows does not erase it either), so the
        // button is drawn in one go into its double buffer, without a flash of another colour.
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        Hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        Hover = false;
        Pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Pressed = e.Button == MouseButtons.Left;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        Pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }
}

/// <summary>What a <see cref="DiscButton"/> shows when it has no picture and no text.</summary>
internal enum DiscGlyph
{
    None,
    Previous, // a triangle pointing left
    Next,     // a triangle pointing right
    Question, // a "?" (the user guide)
    Board,    // a sheet with a few parts on it (the board view), drawn like the mockup's icon
    Cog,      // a cog wheel (the Magic parameters, 2.0.1; only on a Flat disc)
}

/// <summary>
/// A round button like the ones of the Maestro bottom bar: a light-blue disc with a pale ring and a navy picture. The
/// chosen one (<see cref="Selected"/>: the csv table or the board that is shown) gets a thick dark-navy ring (Daan's
/// mockup). <see cref="Dark"/>: a navy disc with a white sign (the user guide "?" at the top right).
/// </summary>
internal class DiscButton : ShapedButton
{
    private bool _selected;

    /// <summary>True: this button chose what is shown now (a thick dark ring).</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
            Parent?.Invalidate(); // the bottom bar draws the white line below the chosen button
        }
    }

    /// <summary>A navy disc with a white sign instead of the light-blue one.</summary>
    internal bool Dark;

    /// <summary>
    /// A flat dark-grey disc of <see cref="FlatDiameter"/> with the sign in the top bar's grey, like the clock sign (Daan,
    /// 2026-11-21: the user-guide "?" "gray looking, like the clock, flat style"). The button can be bigger than the disc,
    /// so it is still easy to hit.
    /// </summary>
    internal bool Flat;

    /// <summary>The diameter of a <see cref="Flat"/> disc, in pixels at 100% (the clock sign's size).</summary>
    internal int FlatDiameter = 18;

    /// <summary>The sign when there is no picture and no text.</summary>
    internal DiscGlyph Glyph = DiscGlyph.None;

    /// <summary>The height of the picture, or the size of the sign, in pixels at 100%.</summary>
    internal int IconHeight = 26;

    /// <summary>
    /// True: the disc is drawn smaller than the button, and full size while <see cref="Selected"/> (Daan, 2026-11-21: the
    /// csv and board buttons bigger "when that menu is opened"; first 10%, then "a bit bigger" again, so it is clear which
    /// view is open: 20%). The button itself keeps its size, so nothing moves.
    /// </summary>
    internal bool GrowWhenSelected;

    /// <summary>How much smaller a disc that can grow is drawn while it is not chosen.</summary>
    internal const float NotSelectedScale = 1 / 1.2f;

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        PaintParentBackground(g);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (Flat)
        {
            // No shadow, no ring, no gradient: one grey disc with the sign cut out in the bar's grey.
            float flat = LogicalToDeviceUnits(FlatDiameter);
            var round = new RectangleF((Width - flat) / 2f, (Height - flat) / 2f, flat, flat);
            using (var grey = new SolidBrush(Pressed ? Theme.WindowButtonPressed : Hover ? Theme.WindowButtonHover : Theme.WindowButton))
            {
                g.FillEllipse(grey, round);
            }

            if (Glyph == DiscGlyph.Cog)
            {
                // The cog of the Magic parameters, right of the "?": the same flat disc, the cog in the bar's grey.
                MaestroDrawing.DrawCog(g, RectangleF.Inflate(round, -flat * 0.19f, -flat * 0.19f), Theme.TopBar,
                    Pressed ? Theme.WindowButtonPressed : Hover ? Theme.WindowButtonHover : Theme.WindowButton);
                return;
            }

            using var signFont = new Font(Font.FontFamily, Font.SizeInPoints, FontStyle.Bold, GraphicsUnit.Point);
            TextRenderer.DrawText(g, Text != "" ? Text : "?", signFont, Rectangle.Round(round), Theme.TopBar,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            return;
        }

        float shadow = LogicalToDeviceUnits(Dark ? 1 : 2);
        float diameter = Math.Min(Width, Height - shadow) - 1;
        if (GrowWhenSelected && !_selected)
        {
            diameter *= NotSelectedScale;
        }

        var circle = new RectangleF((Width - diameter) / 2f, (Height - shadow - diameter) / 2f, diameter, diameter);

        using (var outline = new GraphicsPath())
        {
            outline.AddEllipse(circle);
            MaestroDrawing.FillShadow(g, outline, shadow, 90);
        }

        // The ring: thick and dark for the chosen button, thin and pale for the others; none on a dark disc.
        float ring = _selected ? LogicalToDeviceUnits(6) : Dark ? 0 : LogicalToDeviceUnits(3);
        if (ring > 0)
        {
            using var ringBrush = new SolidBrush(_selected ? Theme.ButtonNavyPressed : Theme.DiscRing);
            g.FillEllipse(ringBrush, circle);
        }

        RectangleF disc = RectangleF.Inflate(circle, -ring, -ring);
        if (Dark)
        {
            MaestroDrawing.FillDisc(g, disc, Hover ? Theme.LogoDiscLight : Theme.DarkDiscLight, Theme.ButtonNavyHover, Theme.ButtonNavy);
        }
        else if (Pressed)
        {
            MaestroDrawing.FillDisc(g, disc, Theme.DiscMiddle, Theme.DiscEdge, Theme.DiscEdge);
        }
        else
        {
            // Daan, 2026-11-21: the white fade less visible. The light spot is only a little lighter than the disc; under
            // the mouse the whole disc gets a little lighter.
            MaestroDrawing.FillDisc(g, disc, Hover ? Theme.DiscLight : Theme.DiscSoftLight, Theme.DiscMiddle, Theme.DiscEdge);
        }

        DrawSign(g, disc);
    }

    private void DrawSign(Graphics g, RectangleF disc)
    {
        Color ink = Dark ? Theme.White : Enabled ? Theme.ButtonNavy : Theme.DiscEdge;
        float size = LogicalToDeviceUnits(IconHeight);
        if (GrowWhenSelected && !_selected)
        {
            size *= NotSelectedScale; // the sign grows with the disc
        }

        float middleX = disc.Left + disc.Width / 2, middleY = disc.Top + disc.Height / 2;

        if (Image != null)
        {
            MaestroDrawing.DrawPicture(g, Image, RectangleF.Inflate(disc, -disc.Width * 0.12f, -disc.Height * 0.12f), size, Enabled);
            return;
        }

        if (Glyph == DiscGlyph.Board)
        {
            var box = new RectangleF(middleX - size / 2, middleY - size / 2, size, size);
            MaestroDrawing.DrawBoardIcon(g, box, ink);
            return;
        }

        if (Text != "" || Glyph == DiscGlyph.Question)
        {
            // "CSV" in bold, like the mockup; the "?" bigger. The text grows with the disc.
            string text = Text != "" ? Text : "?";
            float points = Font.SizeInPoints * (Glyph == DiscGlyph.Question ? 1.5f : 1.25f);
            if (GrowWhenSelected && !_selected)
            {
                points *= NotSelectedScale;
            }

            using var font = new Font(Font.FontFamily, points, FontStyle.Bold, GraphicsUnit.Point);
            TextRenderer.DrawText(g, text, font, Rectangle.Round(disc), ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            return;
        }

        if (Glyph is DiscGlyph.Previous or DiscGlyph.Next)
        {
            // A triangle, its weight a little towards the tip so it looks centred.
            float half = size / 2;
            float direction = Glyph == DiscGlyph.Next ? 1 : -1;
            PointF[] triangle =
            {
                new(middleX - direction * half * 0.55f, middleY - half * 0.8f),
                new(middleX + direction * half * 0.85f, middleY),
                new(middleX - direction * half * 0.55f, middleY + half * 0.8f),
            };
            using var brush = new SolidBrush(ink);
            g.FillPolygon(brush, triangle);
        }
    }
}

/// <summary>
/// A one-line title with a part in bold after it and an extra after that (Daan, 2026-11-21: "Données :
/// DAAN_ROGIERS-P2026.09_001.csv | 13 étiquette(s)", the file name bold). <see cref="Label.Text"/> is the normal part,
/// <see cref="Emphasis"/> the bold part (empty = only the text), <see cref="Suffix"/> what follows it after " | ". When the
/// room is too small, the bold part ends in "…" (the count is then left out).
/// </summary>
internal class TitleLabel : Label
{
    private string _emphasis = "";
    private string _suffix = "";
    private Font? _boldFont;

    /// <summary>After the bold part, in the normal font, behind a " | " (the number of labels); empty = none.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Suffix
    {
        get => _suffix;
        set
        {
            _suffix = value;
            Invalidate();
        }
    }

    /// <summary>The part after the text, in bold; empty = none.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Emphasis
    {
        get => _emphasis;
        set
        {
            _emphasis = value;
            Invalidate();
        }
    }

    private Font BoldFont => _boldFont ??= new Font(Font, FontStyle.Bold);

    protected override void OnFontChanged(EventArgs e)
    {
        _boldFont?.Dispose();
        _boldFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_emphasis == "")
        {
            base.OnPaint(e);
            return;
        }

        Graphics g = e.Graphics;
        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        var unlimited = new Size(int.MaxValue, int.MaxValue);
        string first = Text + " ";
        int firstWidth = TextRenderer.MeasureText(g, first, Font, unlimited, flags).Width;
        TextRenderer.DrawText(g, first, Font, new Rectangle(0, 0, Math.Min(firstWidth, Width), Height), ForeColor, flags);
        if (firstWidth >= Width)
        {
            return;
        }

        // The count stays readable: when the room is short the NAME ends in "…" (it keeps at least a few letters); only in
        // a very narrow column the count goes too.
        string last = _suffix == "" ? "" : "  |  " + _suffix;
        int emphasisWidth = TextRenderer.MeasureText(g, _emphasis, BoldFont, unlimited, flags).Width;
        int lastWidth = last == "" ? 0 : TextRenderer.MeasureText(g, last, Font, unlimited, flags).Width;
        int roomForName = Width - firstWidth - lastWidth;
        bool showCount = last != "" && roomForName >= Math.Min(emphasisWidth, LogicalToDeviceUnits(MinimumNameWidth));
        if (!showCount)
        {
            TextRenderer.DrawText(g, _emphasis, BoldFont, new Rectangle(firstWidth, 0, Width - firstWidth, Height), ForeColor, flags | TextFormatFlags.EndEllipsis);
            return;
        }

        // The name shortened to "DAAN_ROGIERS-P20…" by hand, so the count follows right after it (no gap).
        string name = _emphasis;
        int nameWidth = emphasisWidth;
        while (nameWidth > roomForName && name.Length > 1)
        {
            name = name[..^(name.EndsWith('…') ? 2 : 1)] + "…";
            nameWidth = TextRenderer.MeasureText(g, name, BoldFont, unlimited, flags).Width;
        }

        TextRenderer.DrawText(g, name, BoldFont, new Rectangle(firstWidth, 0, nameWidth, Height), ForeColor, flags);
        TextRenderer.DrawText(g, last, Font, new Rectangle(firstWidth + nameWidth, 0, lastWidth, Height), ForeColor, flags);
    }

    private const int MinimumNameWidth = 80; // at 100%: below this the count is left out, the name gets the room

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _boldFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// The "v" before a file line (Daan, 2026-11-21): a click folds the line up (the sign then points right, ">") or opens it
/// again. Navy on the white panel, as big as a finger can hit.
/// </summary>
internal class FoldButton : Button
{
    private bool _folded;
    private bool _hover;

    public FoldButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    /// <summary>True: the line is folded up (the sign points right).</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Folded
    {
        get => _folded;
        set
        {
            _folded = value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(_hover ? Theme.Field : Parent?.BackColor ?? Theme.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float size = Math.Min(Width, Height) * 0.42f, middleX = Width / 2f, middleY = Height / 2f;
        using var pen = new Pen(Theme.ButtonNavy, Math.Max(2, size * 0.28f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        PointF[] sign = _folded
            ? new[] { new PointF(middleX - size / 4, middleY - size / 2), new PointF(middleX + size / 4, middleY), new PointF(middleX - size / 4, middleY + size / 2) }
            : new[] { new PointF(middleX - size / 2, middleY - size / 4), new PointF(middleX, middleY + size / 4), new PointF(middleX + size / 2, middleY - size / 4) };
        g.DrawLines(pen, sign);
    }
}

/// <summary>What a <see cref="WindowButton"/> does: the three buttons of a window, drawn in the top bar.</summary>
internal enum WindowButtonKind
{
    Minimize,
    Maximize, // shows two squares ("restore") while the window is maximised: see Maximised
    Close,
}

/// <summary>
/// The minimise, maximise and close buttons in the top bar (Daan, 2026-11-21, like Maestro's: a dark grey square with a
/// white sign). Since 2.0.0 the window has no Windows title bar of its own; these replace its buttons.
/// </summary>
internal class WindowButton : Button
{
    private bool _hover;
    private bool _pressed;
    private bool _maximised;

    internal WindowButtonKind Kind;

    public WindowButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    /// <summary>For the maximise button: true while the window is maximised (the sign is then "restore").</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Maximised
    {
        get => _maximised;
        set
        {
            _maximised = value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.TopBar);
        using (var fill = new SolidBrush(_pressed ? Theme.WindowButtonPressed : _hover ? Theme.WindowButtonHover : Theme.WindowButton))
        {
            g.FillRectangle(fill, ClientRectangle);
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        float size = Math.Min(Width, Height) * 0.5f;
        float left = (Width - size) / 2, top = (Height - size) / 2;
        float thick = Math.Max(2, size * 0.2f);
        using var pen = new Pen(Theme.White, thick) { StartCap = LineCap.Square, EndCap = LineCap.Square };

        switch (Kind)
        {
            case WindowButtonKind.Minimize:
                using (var white = new SolidBrush(Theme.White))
                {
                    g.FillRectangle(white, left, top + (size - thick) / 2, size, thick);
                }

                break;
            case WindowButtonKind.Close:
                pen.Width = thick * 1.2f;
                g.DrawLine(pen, left + thick / 2, top + thick / 2, left + size - thick / 2, top + size - thick / 2);
                g.DrawLine(pen, left + size - thick / 2, top + thick / 2, left + thick / 2, top + size - thick / 2);
                break;
            default:
                pen.Width = thick * 0.6f;
                if (_maximised)
                {
                    // "restore": two squares, the back one up and to the right
                    float small = size * 0.72f, shift = size - small;
                    g.DrawLines(pen, new[] { new PointF(left + shift, top + shift), new PointF(left + shift, top), new PointF(left + size, top), new PointF(left + size, top + small), new PointF(left + small, top + small) });
                    g.DrawRectangle(pen, left, top + shift, small, small);
                }
                else
                {
                    g.DrawRectangle(pen, left, top, size, size);
                    g.DrawLine(pen, left, top + pen.Width, left + size, top + pen.Width); // the thicker top edge of a window
                }

                break;
        }
    }
}

/// <summary>
/// The clock in the top bar (Daan, 2026-11-21, like Maestro's: a round clock sign and the time "12:23"). The window sets
/// <see cref="Time"/> every few seconds; the format is the same in every language.
/// </summary>
internal class ClockDisplay : Control
{
    private const int IconSize = 18; // at 100%
    private const int Gap = 8;

    // The digits as tall as the clock sign (Daan, 2026-11-21). A digit of Verdana is about 0.73 of the font's em size.
    private const float DigitHeightPerEm = 0.73f;

    private string _time = "";
    private Font? _timeFont;

    /// <summary>The font of the time: its digits are as high as the clock sign.</summary>
    internal Font TimeFont => _timeFont ??= new Font(Font.FontFamily, LogicalToDeviceUnits(IconSize) / DigitHeightPerEm, FontStyle.Regular, GraphicsUnit.Pixel);

    protected override void OnFontChanged(EventArgs e)
    {
        _timeFont?.Dispose();
        _timeFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        _timeFont?.Dispose();
        _timeFont = null; // the sign's size in pixels changed
        base.OnDpiChangedAfterParent(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timeFont?.Dispose();
        }

        base.Dispose(disposing);
    }

    public ClockDisplay()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false;
    }

    /// <summary>The time shown, e.g. "12:23".</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Time
    {
        get => _time;
        set
        {
            if (_time != value)
            {
                _time = value;
                Invalidate();
            }
        }
    }

    /// <summary>The width for the sign and "88:88" (the time never makes it jump), in pixels.</summary>
    internal int WidthNeeded() =>
        LogicalToDeviceUnits(IconSize + Gap) + TextRenderer.MeasureText("88:88", TimeFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + LogicalToDeviceUnits(2);

    /// <summary>The height for the time's font, in pixels.</summary>
    internal int HeightNeeded() => TimeFont.Height + LogicalToDeviceUnits(4);

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.TopBar);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float icon = LogicalToDeviceUnits(IconSize);
        var circle = new RectangleF(0, (Height - icon) / 2, icon, icon);
        using (var dark = new SolidBrush(Theme.WindowButton))
        {
            g.FillEllipse(dark, circle);
        }

        // The hands: twelve o'clock and three o'clock.
        float middleX = circle.Left + icon / 2, middleY = circle.Top + icon / 2;
        using (var hand = new Pen(Theme.TopBar, Math.Max(1.5f, icon / 9)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(hand, middleX, middleY, middleX, circle.Top + icon * 0.22f);
            g.DrawLine(hand, middleX, middleY, circle.Left + icon * 0.74f, middleY);
        }

        int textLeft = (int)(icon + LogicalToDeviceUnits(Gap));
        TextRenderer.DrawText(g, _time, TimeFont, new Rectangle(textLeft, 0, Math.Max(0, Width - textLeft), Height), Theme.WindowButton,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }
}

/// <summary>
/// A tab in the title of a panel, like Maestro's "OM | OM+GOTO+ERR": the chosen one is white with bold navy text and a
/// light-blue line at the bottom, the others are grey (<see cref="IdleFill"/>). Its width does not change when it is
/// chosen (<see cref="WidthNeeded"/> measures the bold text).
/// </summary>
internal class TabButton : Button
{
    private const int SidePadding = 14; // at 100%, left and right of the text
    private bool _selected;
    private bool _hover;
    private Font? _boldFont;

    /// <summary>The fill when it is not chosen.</summary>
    internal Color IdleFill = Theme.TabIdle;

    public TabButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    /// <summary>True: what this tab shows is shown now.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    private Font BoldFont => _boldFont ??= new Font(Font, FontStyle.Bold);

    /// <summary>The width this tab needs for its text in bold (chosen), with room on both sides, in pixels.</summary>
    internal int WidthNeeded() =>
        TextRenderer.MeasureText(Text, BoldFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width +
        LogicalToDeviceUnits(2 * SidePadding);

    protected override void OnFontChanged(EventArgs e)
    {
        _boldFont?.Dispose();
        _boldFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        Color fill = _selected ? Theme.White : _hover ? Theme.Field : IdleFill;
        g.Clear(fill);

        if (_selected)
        {
            int line = LogicalToDeviceUnits(3);
            using var underline = new SolidBrush(Theme.Selection);
            g.FillRectangle(underline, 0, Height - line, Width, line);
        }

        TextRenderer.DrawText(g, Text, _selected ? BoldFont : Font, ClientRectangle, _selected ? Theme.ButtonNavy : Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _boldFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>What a <see cref="PillButton"/> shows in its navy circle.</summary>
internal enum PillSign
{
    Printer, // the print button
    Tick,    // Save of the Magic parameters (2.0.1)
    None,    // Cancel of the Magic parameters: no circle, the text in the middle, not bold
}

/// <summary>
/// The print button at the bottom right, like Maestro's "MDI" / "Manual" buttons: a light-grey pill with a navy circle
/// holding the white printer of the mockup (<see cref="MaestroDrawing.DrawPrinterIcon"/>), and the bold text "Print".
/// Since 2.0.1 also Save (<see cref="PillSign.Tick"/>) and Cancel (<see cref="PillSign.None"/>) of the Magic parameters.
/// </summary>
internal class PillButton : ShapedButton
{
    private const int CircleMargin = 6; // between the edge of the pill and the navy circle, at 100%
    private const int TextGap = 12;     // between the circle and the text, at 100%
    private const int RightPadding = 26;

    /// <summary>The size of the printer in the circle, in pixels at 100%.</summary>
    internal int IconHeight = 24;

    /// <summary>The sign in the navy circle (the printer for the print button).</summary>
    internal PillSign Sign = PillSign.Printer;

    private Font BoldFont => new(Font, FontStyle.Bold);

    /// <summary>The width this button needs for its circle and its text, in pixels.</summary>
    internal int WidthNeeded()
    {
        if (Sign == PillSign.None)
        {
            int plain = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
            return plain + 2 * LogicalToDeviceUnits(RightPadding);
        }

        using Font bold = BoldFont;
        int text = TextRenderer.MeasureText(Text, bold, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
        return Height + LogicalToDeviceUnits(TextGap - CircleMargin) + text + LogicalToDeviceUnits(RightPadding);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        PaintParentBackground(g);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float shadow = LogicalToDeviceUnits(2);
        var pill = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1 - shadow);
        using (GraphicsPath path = MaestroDrawing.Pill(pill))
        {
            MaestroDrawing.FillShadow(g, path, shadow, 80);
            using var fill = new SolidBrush(Pressed ? Theme.TabIdle : Hover ? Theme.White : Theme.Pill);
            g.FillPath(fill, path);

            if (Focused && ShowFocusCues)
            {
                using var ring = new Pen(Theme.ButtonNavy, 1.5f) { DashStyle = DashStyle.Dot };
                g.DrawPath(ring, path);
            }
        }

        if (Sign == PillSign.None)
        {
            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(pill), Enabled ? Theme.ButtonNavy : Theme.DisabledText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            return;
        }

        float margin = LogicalToDeviceUnits(CircleMargin);
        float diameter = pill.Height - 2 * margin;
        var circle = new RectangleF(pill.Left + margin, pill.Top + margin, diameter, diameter);
        using (var navy = new SolidBrush(Enabled ? Theme.ButtonNavy : Theme.XmlTabIdle))
        {
            g.FillEllipse(navy, circle);
        }

        if (Sign == PillSign.Tick)
        {
            float tick = diameter * 0.5f; // the tick of TsDssConverter's Save: half the circle
            MaestroDrawing.DrawTick(g, new RectangleF(circle.Left + (circle.Width - tick) / 2, circle.Top + (circle.Height - tick) / 2, tick, tick), Theme.White);
        }
        else
        {
            float icon = LogicalToDeviceUnits(IconHeight);
            MaestroDrawing.DrawPrinterIcon(g, new RectangleF(circle.Left + (circle.Width - icon) / 2, circle.Top + (circle.Height - icon) / 2, icon, icon), Theme.White);
        }

        using Font bold = BoldFont;
        int textLeft = (int)(circle.Right + LogicalToDeviceUnits(TextGap));
        var textArea = new Rectangle(textLeft, 0, Math.Max(0, Width - textLeft), (int)pill.Height);
        TextRenderer.DrawText(g, Text, bold, textArea, Enabled ? Theme.ButtonNavy : Theme.DisabledText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

/// <summary>
/// The top left of the window, like Maestro's white tab with the round check: a white tab with a round right end and a
/// shadow, holding the app logo on a round blue disc, the program name and one line below it. It replaces the blue
/// banner of 1.x. When the window is narrow the line below the name ends in "…".
/// </summary>
internal class TitleTab : Control
{
    private const int LeftPadding = 14;
    private const int DiscSize = 50;
    private const int TextGap = 14;
    private const int RightPadding = 34;
    private const int TabTop = 0;    // the tab sticks to the top of the window (Daan, 2026-11-21: no grey above it)
    private const int TabBottom = 8; // room below it, for the shadow

    private Font? _titleFont;
    private Font? _subtitleFont;

    /// <summary>The line below the program name.</summary>
    internal string Subtitle = "";

    public TitleTab()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false;
    }

    // The name 2 pt bigger than at first (Daan, 2026-11-21: "some points bigger, if it is 12 pt make it 14 pt"); the line
    // below it in bold (Daan, the same day).
    internal Font TitleFont => _titleFont ??= Theme.CreateFont(Font, Font.SizeInPoints + 7, FontStyle.Bold);
    internal Font SubtitleFont => _subtitleFont ??= Theme.CreateFont(Font, Font.SizeInPoints - 1, FontStyle.Bold);

    protected override void OnFontChanged(EventArgs e)
    {
        _titleFont?.Dispose();
        _subtitleFont?.Dispose();
        _titleFont = null;
        _subtitleFont = null;
        base.OnFontChanged(e);
    }

    /// <summary>The width of the white tab when everything fits, in pixels.</summary>
    internal int WidthNeeded()
    {
        int title = TextRenderer.MeasureText(Text, TitleFont, new Size(int.MaxValue, int.MaxValue), TextFlags).Width;
        int subtitle = TextRenderer.MeasureText(Subtitle, SubtitleFont, new Size(int.MaxValue, int.MaxValue), TextFlags).Width;
        return LogicalToDeviceUnits(LeftPadding + DiscSize + TextGap + RightPadding) + Math.Max(title, subtitle);
    }

    /// <summary>The white tab: as wide as its content, or as the control when that is narrower.</summary>
    internal Rectangle TabBounds()
    {
        int top = LogicalToDeviceUnits(TabTop);
        return new Rectangle(0, top, Math.Min(Width, WidthNeeded()), Math.Max(1, Height - top - LogicalToDeviceUnits(TabBottom)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Theme.TopBar);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // The tab: square on the left (it starts at the window's edge), a half circle on the right.
        Rectangle tab = TabBounds();
        using (var path = new GraphicsPath())
        {
            float diameter = tab.Height;
            path.AddLine(tab.Left - 1, tab.Top, tab.Right - diameter / 2, tab.Top);
            path.AddArc(tab.Right - diameter, tab.Top, diameter, diameter, 270, 180);
            path.AddLine(tab.Right - diameter / 2, tab.Bottom, tab.Left - 1, tab.Bottom);
            path.CloseFigure();
            MaestroDrawing.FillShadow(g, path, LogicalToDeviceUnits(3), 60);
            using var white = new SolidBrush(Theme.White);
            g.FillPath(white, path);
        }

        // The logo on its round blue disc.
        float disc = LogicalToDeviceUnits(DiscSize);
        var circle = new RectangleF(LogicalToDeviceUnits(LeftPadding), tab.Top + (tab.Height - disc) / 2f, disc, disc);
        MaestroDrawing.FillDisc(g, circle, Theme.LogoDiscLight, Theme.LogoDiscMiddle, Theme.LogoDiscEdge);
        // The logo 72% of the disc (Daan, 2026-11-21: 20% bigger than the 60% of before).
        AppLogo.Draw(g, RectangleF.Inflate(circle, -disc * 0.14f, -disc * 0.14f), withBackground: false);

        // The name and the line below it, together in the middle of the tab's height.
        int textLeft = (int)(circle.Right + LogicalToDeviceUnits(TextGap));
        int textWidth = Math.Max(0, tab.Right - LogicalToDeviceUnits(RightPadding) / 2 - textLeft);
        int titleHeight = TitleFont.Height, subtitleHeight = SubtitleFont.Height;
        int top = tab.Top + (tab.Height - titleHeight - subtitleHeight) / 2;
        TextRenderer.DrawText(g, Text, TitleFont, new Rectangle(textLeft, top, textWidth, titleHeight), Theme.ButtonNavy, TextFlags);
        TextRenderer.DrawText(g, Subtitle, SubtitleFont, new Rectangle(textLeft, top + titleHeight, textWidth, subtitleHeight), Theme.TextSoft, TextFlags);
    }

    // NoPadding: without it Windows puts room before a text that grows with the font, so the big name started further
    // right than the line below it (Daan, 2026-11-21). Now both start at the same left edge.
    private const TextFormatFlags TextFlags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont?.Dispose();
            _subtitleFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// A white tile below the preview, like Maestro's speed tiles: a title, a big light-blue value and two small lines
/// (e.g. "Position" / "X: 410 | Y: 1787" / "mm" / "LabelID 1608 | rotation 0°"). Its height follows its fonts.
/// </summary>
internal class InfoTile : Control
{
    private const int PaddingX = 16;
    private const int PaddingY = 10;
    private const int LineGap = 2;

    private string _caption = "", _value = "", _line1 = "", _line2 = "";
    private Font? _valueFont;
    private Font? _smallFont;

    public InfoTile()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false;
        BackColor = Theme.White;
    }

    // (The two attributes tell the Visual Studio designer to ignore these properties, like LabelPreview's.)

    /// <summary>The title of the tile ("Position").</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string Caption { get => _caption; set { _caption = value; Invalidate(); } }

    /// <summary>The big value ("X: 410 | Y: 1787").</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string Value { get => _value; set { _value = value; Invalidate(); } }

    /// <summary>The first small line ("mm").</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string Line1 { get => _line1; set { _line1 = value; Invalidate(); } }

    /// <summary>The second small line ("LabelID 1608 | rotation 0°"); may be empty.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string Line2 { get => _line2; set { _line2 = value; Invalidate(); } }

    internal Font ValueFont => _valueFont ??= Theme.CreateFont(Font, Font.SizeInPoints + 5, FontStyle.Bold);
    internal Font SmallFont => _smallFont ??= Theme.CreateFont(Font, Font.SizeInPoints - 0.5f);

    /// <summary>The height of the tile for its fonts: the title, the value and two small lines, in pixels.</summary>
    internal int HeightNeeded() =>
        2 * LogicalToDeviceUnits(PaddingY) + Font.Height + ValueFont.Height + 2 * SmallFont.Height + 3 * LogicalToDeviceUnits(LineGap);

    protected override void OnFontChanged(EventArgs e)
    {
        _valueFont?.Dispose();
        _smallFont?.Dispose();
        _valueFont = null;
        _smallFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Theme.White);
        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        int left = LogicalToDeviceUnits(PaddingX), width = Math.Max(0, Width - 2 * left), gap = LogicalToDeviceUnits(LineGap);
        int top = LogicalToDeviceUnits(PaddingY);
        TextRenderer.DrawText(g, _caption, Font, new Rectangle(left, top, width, Font.Height), Theme.Text, flags);
        top += Font.Height + gap;

        // The value as big as it is meant to be; in a narrow tile a smaller font, so the numbers are never cut off
        // (not below the size of the title).
        Font valueFont = ValueFont;
        Font? smaller = null;
        while (TextRenderer.MeasureText(g, _value, valueFont).Width > width && valueFont.SizeInPoints > Font.SizeInPoints)
        {
            smaller?.Dispose();
            smaller = Theme.CreateFont(Font, valueFont.SizeInPoints - 0.5f, FontStyle.Bold);
            valueFont = smaller;
        }

        var valueArea = new Rectangle(left, top, width, ValueFont.Height);
        TextRenderer.DrawText(g, _value, valueFont, valueArea, Theme.TileValue, flags | TextFormatFlags.VerticalCenter);
        smaller?.Dispose();
        top += ValueFont.Height + gap;
        TextRenderer.DrawText(g, _line1, SmallFont, new Rectangle(left, top, width, SmallFont.Height), Theme.TileLine, flags);
        top += SmallFont.Height + gap;
        TextRenderer.DrawText(g, _line2, SmallFont, new Rectangle(left, top, width, SmallFont.Height), Theme.TileLine, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _valueFont?.Dispose();
            _smallFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// The bar at the bottom of the window, like Maestro's: a blue band (a gradient, with a round right end) holding the
/// round buttons — the csv table and the board at the left, previous / counter / next at its right end — and, on the grey
/// to the right of it, the print button. The buttons are placed here in code (<see cref="OnLayout"/>), not by a table:
/// the band's end depends on where the print button is.
/// </summary>
internal class BottomBar : Control, IPaintsBackgroundForChildren
{
    /// <summary>The height of the bar at 100%.</summary>
    public const int BarHeight = 84;

    private const int Padding24 = 24;  // at both ends, and between the band and the print button
    private const int DiscGap = 18;    // between the csv and the board button
    private const int CounterGap = 8;  // between the arrows and the counter

    private readonly DiscButton _csv, _board, _previous, _next;
    private readonly Label _counter;
    private readonly PillButton _print;

    public BottomBar(DiscButton csv, DiscButton board, DiscButton previous, Label counter, DiscButton next, PillButton print)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false;
        (_csv, _board, _previous, _counter, _next, _print) = (csv, board, previous, counter, next, print);
        Controls.AddRange(new Control[] { csv, board, previous, counter, next, print });
    }

    /// <summary>Where the blue band ends (its round end included), in the bar's pixels. Never moves when print is hidden.</summary>
    internal int BandRight { get; private set; }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int pad = LogicalToDeviceUnits(Padding24);
        int middle = Height / 2;

        _print.Location = new Point(Width - pad - _print.Width, middle - _print.Height / 2);
        BandRight = Math.Max(Height, _print.Left - pad);

        // The csv and board buttons from the left; a hidden one leaves no gap.
        int x = pad;
        foreach (DiscButton disc in new[] { _csv, _board })
        {
            disc.Location = new Point(x, middle - disc.Height / 2);
            if (disc.Visible)
            {
                x += disc.Width + LogicalToDeviceUnits(DiscGap);
            }
        }

        // The arrows at the round end of the band: "next" in the middle of the half circle, the counter and "previous"
        // to its left. They never come over the csv and board buttons (a narrow window: they move right, into the grey).
        int gap = LogicalToDeviceUnits(CounterGap);
        int nextLeft = BandRight - Height / 2 - _next.Width / 2;
        int needed = _previous.Width + gap + _counter.Width + gap + _next.Width;
        nextLeft = Math.Max(nextLeft, x + needed - _next.Width);
        _next.Location = new Point(nextLeft, middle - _next.Height / 2);
        _counter.Location = new Point(_next.Left - gap - _counter.Width, middle - _counter.Height / 2);
        _previous.Location = new Point(_counter.Left - gap - _previous.Width, middle - _previous.Height / 2);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        DrawBar(e.Graphics);
    }

    /// <summary>The bar's background inside one of its round buttons: the same drawing, moved to the button's place.</summary>
    public void PaintBackgroundFor(Control child, Graphics g)
    {
        GraphicsState state = g.Save();
        g.TranslateTransform(-child.Left, -child.Top);
        DrawBar(g);
        g.Restore(state);
    }

    /// <summary>The grey bar with the blue band (a gradient, its right end a half circle).</summary>
    private void DrawBar(Graphics g)
    {
        using (var grey = new SolidBrush(Theme.WorkArea))
        {
            g.FillRectangle(grey, 0, 0, Width, Height); // not Clear: that would ignore the move for a child
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;

        float diameter = Height;
        using var band = new GraphicsPath();
        band.AddLine(-1, 0, BandRight - diameter / 2, 0);
        band.AddArc(BandRight - diameter, 0, diameter, diameter - 1, 270, 180);
        band.AddLine(BandRight - diameter / 2, Height, -1, Height);
        band.CloseFigure();
        using var gradient = new LinearGradientBrush(new Point(0, -1), new Point(0, Height + 1), Theme.BarTop, Theme.BarBottom);
        g.FillPath(gradient, band);

        // A short white line below the chosen round button, like Maestro's bar (Daan, 2026-11-21): which view is open.
        foreach (DiscButton disc in new[] { _csv, _board })
        {
            if (disc.Visible && disc.Selected)
            {
                float width = LogicalToDeviceUnits(SelectedLineWidth), height = LogicalToDeviceUnits(SelectedLineHeight);
                var line = new RectangleF(disc.Left + (disc.Width - width) / 2, disc.Bottom + (Height - disc.Bottom - height) / 2, width, height);
                using GraphicsPath rounded = MaestroDrawing.Pill(line);
                using var white = new SolidBrush(Theme.White);
                g.FillPath(white, rounded);
            }
        }
    }

    private const int SelectedLineWidth = 22;  // the white line below the chosen button, at 100%
    private const int SelectedLineHeight = 4;

    protected override void OnPaint(PaintEventArgs e)
    {
        // Everything is in the background (the buttons on it paint themselves).
    }
}
