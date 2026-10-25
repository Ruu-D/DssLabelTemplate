using System.Drawing.Drawing2D;
using System.Xml.Linq;

namespace DssLabelTemplate;

/// <summary>
/// Draws the print objects of one &lt;PrintLabel&gt; (DSS Software Interface, "Label-xml example"): Text, Line, Rectangle,
/// Image, Barcode, QRCode and Arrow. The caller sets up the Graphics so that one unit is one millimetre and (0, 0) is the
/// top left corner of the label; the manual gives every coordinate in mm from there, Y downwards.
///
/// What the manual does not say, and what this preview therefore ASSUMES (open point, to check on a printed label):
///   - Text X, Y is the top left corner of the text, and a rotated text turns around that point;
///   - Rotation is counter-clockwise; a rotated barcode keeps its top left corner at X, Y (it stays inside X..X+Height);
///   - the default font is Arial 10 pt ("defined by DSSPrintServer").
/// </summary>
internal static class LabelRenderer
{
    private const float MillimetresPerPoint = 25.4f / 72f;
    private const string DefaultFont = "Arial";
    private const float DefaultFontSize = 10f; // points

    // A print object may touch the edge of the label: rounding is not "outside".
    private const float EdgeTolerance = 0.01f;

    /// <summary>
    /// The area (mm) every print object covers, as far as it can be known: a text only counts with its start point (its
    /// width depends on the font). Objects that cannot be drawn are left out.
    /// </summary>
    public static List<RectangleF> ObjectBounds(LabelItem label)
    {
        var boxes = new List<RectangleF>();
        foreach (XElement element in label.Element.Elements())
        {
            if (Bounds(element, label) is RectangleF box)
            {
                boxes.Add(box);
            }
        }

        return boxes;
    }

    /// <summary>
    /// The area to show: the label itself (0, 0 – length x width) plus every object that sticks out of it, so a mistake
    /// in a coordinate is visible instead of falling off the screen.
    /// </summary>
    public static RectangleF ViewArea(IEnumerable<RectangleF> objects, SizeF labelSize)
    {
        var view = new RectangleF(PointF.Empty, labelSize);
        foreach (RectangleF box in objects)
        {
            view = RectangleF.Union(view, box);
        }

        return view;
    }

    /// <summary>How many objects (partly) lie outside a label of this size.</summary>
    public static int CountOutside(IEnumerable<RectangleF> objects, SizeF labelSize)
    {
        return objects.Count(box => box.Left < -EdgeTolerance || box.Top < -EdgeTolerance
            || box.Right > labelSize.Width + EdgeTolerance || box.Bottom > labelSize.Height + EdgeTolerance);
    }

    /// <summary>
    /// Draws every print object. An object with a missing or wrong number is skipped and named in <paramref name="problems"/>.
    /// Unknown objects are skipped silently (they may be new in DSS).
    /// </summary>
    /// <param name="baseFolder">The folder of the label file: an Image may have a path relative to it.</param>
    public static void Draw(Graphics g, LabelItem label, string? baseFolder, LabelImages images, List<string> problems)
    {
        foreach (XElement element in label.Element.Elements())
        {
            bool drawn = element.Name.LocalName.ToUpperInvariant() switch
            {
                "TEXT" => DrawText(g, element, label),
                "LINE" => DrawLine(g, element, label),
                "RECTANGLE" => DrawRectangle(g, element, label),
                "IMAGE" => DrawImage(g, element, label, baseFolder, images),
                "BARCODE" => DrawBarcode(g, element, label),
                "QRCODE" => DrawQrCode(g, element, label),
                "ARROW" => DrawArrow(g, element, label),
                _ => true,
            };

            if (!drawn)
            {
                problems.Add(Strings.ObjectNotDrawn(element.Name.LocalName, LabelXml.LineOf(element)));
            }
        }
    }

    // ------------------------------------------------------------------ the objects

    private static bool DrawText(Graphics g, XElement element, LabelItem label)
    {
        if (Number(element, label, "X") is not float x || Number(element, label, "Y") is not float y)
        {
            return false;
        }

        float size = Number(element, label, "FontSize") ?? DefaultFontSize;
        if (size <= 0)
        {
            return false;
        }

        // The manual names the attribute "FontStyle"; its template example writes Style="BOLD". Both are read.
        FontStyle style = ParseFontStyle(label.Value(element, "FontStyle") ?? label.Value(element, "Style"));
        string family = label.Value(element, "Font") ?? DefaultFont;
        float rotation = Number(element, label, "Rotation") ?? 0;
        string text = label.Variables.Resolve(element.Value);

        // GraphicsUnit.World: the font size is in the units of the drawing, mm.
        using var font = new Font(family, size * MillimetresPerPoint, style, GraphicsUnit.World);
        using var ink = new SolidBrush(Theme.LabelInk);
        GraphicsState state = g.Save();
        g.TranslateTransform(x, y);
        g.RotateTransform(-rotation); // counter-clockwise (GDI+ turns clockwise)
        g.DrawString(text, font, ink, 0, 0, StringFormat.GenericTypographic);
        g.Restore(state);
        return true;
    }

    private static bool DrawLine(Graphics g, XElement element, LabelItem label)
    {
        if (Corners(element, label) is not (float x, float y, float x2, float y2))
        {
            return false;
        }

        using Pen pen = InkPen(element, label);
        g.DrawLine(pen, x, y, x2, y2);
        return true;
    }

    private static bool DrawRectangle(Graphics g, XElement element, LabelItem label)
    {
        if (Corners(element, label) is not (float x, float y, float x2, float y2))
        {
            return false;
        }

        using Pen pen = InkPen(element, label);
        RectangleF box = Normalised(x, y, x2, y2);
        g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
        return true;
    }

    private static bool DrawImage(Graphics g, XElement element, LabelItem label, string? baseFolder, LabelImages images)
    {
        if (Corners(element, label) is not (float x, float y, float x2, float y2))
        {
            return false;
        }

        RectangleF box = Normalised(x, y, x2, y2);
        string path = label.Variables.Resolve(element.Value).Trim();
        Image? picture = images.Get(ResolvePath(path, baseFolder));

        if (picture != null)
        {
            g.DrawImage(picture, box);
            return true;
        }

        // The picture is not on this PC (e.g. C:\DSS\... of the label PC): a placeholder with its file name.
        using (var fill = new SolidBrush(Theme.VeryPaleBlue))
        using (var pen = new Pen(Theme.LightBlue, 0.3f))
        {
            g.FillRectangle(fill, box);
            g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
            g.DrawLine(pen, box.Left, box.Top, box.Right, box.Bottom);
            g.DrawLine(pen, box.Left, box.Bottom, box.Right, box.Top);
        }

        using var font = new Font(DefaultFont, 2.5f, FontStyle.Regular, GraphicsUnit.World);
        using var navy = new SolidBrush(Theme.Navy);
        using var centred = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisPath };
        g.DrawString(Path.GetFileName(path), font, navy, box, centred);
        return true;
    }

    private static bool DrawBarcode(Graphics g, XElement element, LabelItem label)
    {
        if (Number(element, label, "X") is not float x || Number(element, label, "Y") is not float y
            || Number(element, label, "Width") is not float width || Number(element, label, "Height") is not float height
            || width <= 0 || height <= 0)
        {
            return false;
        }

        float rotation = Number(element, label, "Rotation") ?? 0;
        bool[] modules = Code128.Modules(label.Variables.Resolve(element.Value).Trim());
        float module = width / modules.Length;

        using var ink = new SolidBrush(Theme.LabelInk);
        GraphicsState state = g.Save();
        RectangleF turned = RotatedBounds(width, height, rotation);
        g.TranslateTransform(x - turned.X, y - turned.Y); // the turned barcode starts at X, Y
        g.RotateTransform(-rotation);

        for (int i = 0; i < modules.Length; i++)
        {
            if (!modules[i])
            {
                continue;
            }

            int run = 1;
            while (i + run < modules.Length && modules[i + run])
            {
                run++;
            }

            g.FillRectangle(ink, i * module, 0, run * module, height);
            i += run - 1;
        }

        g.Restore(state);
        return true;
    }

    /// <summary>
    /// A QR code is drawn as a pattern that LOOKS like one (the three corner squares and modules that follow the text),
    /// in the right place and size. It is not a real, scannable QR code: that is made by DSSPrintServer.
    /// </summary>
    private static bool DrawQrCode(Graphics g, XElement element, LabelItem label)
    {
        if (Number(element, label, "X") is not float x || Number(element, label, "Y") is not float y
            || Number(element, label, "Width") is not float width || Number(element, label, "Height") is not float height
            || width <= 0 || height <= 0)
        {
            return false;
        }

        const int count = 21; // the smallest QR code is 21 x 21 modules
        float cell = Math.Min(width, height) / count;
        string text = label.Variables.Resolve(element.Value).Trim();
        uint hash = 2166136261; // FNV-1a: the same text always gives the same pattern (string.GetHashCode does not)
        foreach (char c in text)
        {
            hash = (hash ^ c) * 16777619;
        }

        using var ink = new SolidBrush(Theme.LabelInk);
        using var paper = new SolidBrush(Theme.LabelPaper);
        for (int row = 0; row < count; row++)
        {
            for (int column = 0; column < count; column++)
            {
                if (InFinder(column, row))
                {
                    continue;
                }

                hash = (hash ^ (uint)(row * count + column)) * 16777619;
                if ((hash >> 13 & 1) == 1)
                {
                    g.FillRectangle(ink, x + column * cell, y + row * cell, cell, cell);
                }
            }
        }

        foreach ((int column, int row) in new[] { (0, 0), (count - 7, 0), (0, count - 7) })
        {
            g.FillRectangle(ink, x + column * cell, y + row * cell, 7 * cell, 7 * cell);
            g.FillRectangle(paper, x + (column + 1) * cell, y + (row + 1) * cell, 5 * cell, 5 * cell);
            g.FillRectangle(ink, x + (column + 2) * cell, y + (row + 2) * cell, 3 * cell, 3 * cell);
        }

        return true;

        static bool InFinder(int column, int row) =>
            (column < 8 && row < 8) || (column >= count - 8 && row < 8) || (column < 8 && row >= count - 8);
    }

    /// <summary>
    /// An arrow inside its box, pointing to one of 8 places (Direction: 1 = lower left, 2 = middle left, 3 = top left,
    /// 4 = top middle, 5 = top right, 6 = middle right (default), 7 = lower right, 8 = lower middle), filled with one of
    /// the 8 fill styles of the manual.
    /// </summary>
    private static bool DrawArrow(Graphics g, XElement element, LabelItem label)
    {
        if (Corners(element, label) is not (float x, float y, float x2, float y2))
        {
            return false;
        }

        RectangleF box = Normalised(x, y, x2, y2);
        int direction = (int)(Number(element, label, "Direction") ?? 6);
        float middleX = box.Left + box.Width / 2;
        float middleY = box.Top + box.Height / 2;

        PointF tip = direction switch
        {
            1 => new PointF(box.Left, box.Bottom),
            2 => new PointF(box.Left, middleY),
            3 => new PointF(box.Left, box.Top),
            4 => new PointF(middleX, box.Top),
            5 => new PointF(box.Right, box.Top),
            7 => new PointF(box.Right, box.Bottom),
            8 => new PointF(middleX, box.Bottom),
            _ => new PointF(box.Right, middleY),
        };
        var tail = new PointF(2 * middleX - tip.X, 2 * middleY - tip.Y); // the opposite point of the box

        float length = (float)Math.Sqrt((tip.X - tail.X) * (tip.X - tail.X) + (tip.Y - tail.Y) * (tip.Y - tail.Y));
        if (length <= 0)
        {
            return true;
        }

        // How thick the arrow is: the size of the box across the arrow.
        float across = direction switch
        {
            2 or 6 => box.Height,
            4 or 8 => box.Width,
            _ => Math.Min(box.Width, box.Height) * 0.7f,
        };
        float headLength = Math.Min(length * 0.45f, across * 0.9f);
        float headHalf = across * 0.45f;
        float shaftHalf = across * 0.18f;

        // The arrow along the x axis from 0 to its length, then turned and moved onto the box.
        PointF[] shape =
        {
            new(0, -shaftHalf), new(length - headLength, -shaftHalf), new(length - headLength, -headHalf), new(length, 0),
            new(length - headLength, headHalf), new(length - headLength, shaftHalf), new(0, shaftHalf),
        };
        using (var place = new Matrix())
        {
            place.Translate(tail.X, tail.Y);
            place.Rotate((float)(Math.Atan2(tip.Y - tail.Y, tip.X - tail.X) * 180 / Math.PI));
            place.TransformPoints(shape);
        }

        int fillStyle = (int)(Number(element, label, "FillStyle") ?? 0);
        using (Brush? fill = ArrowFill(fillStyle))
        {
            if (fill != null)
            {
                g.FillPolygon(fill, shape);
            }
        }

        using Pen pen = InkPen(element, label);
        g.DrawPolygon(pen, shape);
        return true;
    }

    /// <summary>0 = solid, 1 = clear (only the outline), 2 – 7 = hatched: horizontal, vertical, two diagonals, cross, diagonal cross.</summary>
    private static Brush? ArrowFill(int fillStyle)
    {
        return fillStyle switch
        {
            1 => null,
            2 => new HatchBrush(HatchStyle.Horizontal, Theme.LabelInk, Color.Transparent),
            3 => new HatchBrush(HatchStyle.Vertical, Theme.LabelInk, Color.Transparent),
            4 => new HatchBrush(HatchStyle.ForwardDiagonal, Theme.LabelInk, Color.Transparent),
            5 => new HatchBrush(HatchStyle.BackwardDiagonal, Theme.LabelInk, Color.Transparent),
            6 => new HatchBrush(HatchStyle.Cross, Theme.LabelInk, Color.Transparent),
            7 => new HatchBrush(HatchStyle.DiagonalCross, Theme.LabelInk, Color.Transparent),
            _ => new SolidBrush(Theme.LabelInk),
        };
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>The area an object covers (mm), to size the label. A text has no size here (only its start point).</summary>
    private static RectangleF? Bounds(XElement element, LabelItem label)
    {
        string name = element.Name.LocalName.ToUpperInvariant();
        if (name is "LINE" or "RECTANGLE" or "IMAGE" or "ARROW")
        {
            return Corners(element, label) is (float x, float y, float x2, float y2) ? Normalised(x, y, x2, y2) : null;
        }

        if (Number(element, label, "X") is not float left || Number(element, label, "Y") is not float top)
        {
            return null;
        }

        if (name is "BARCODE" or "QRCODE"
            && Number(element, label, "Width") is float width && Number(element, label, "Height") is float height)
        {
            if (name == "BARCODE")
            {
                // A rotated barcode covers the box of its turned rectangle, with that box's top left corner at X, Y.
                RectangleF turned = RotatedBounds(width, height, Number(element, label, "Rotation") ?? 0);
                return new RectangleF(left, top, turned.Width, turned.Height);
            }

            float side = Math.Min(width, height);
            return new RectangleF(left, top, side, side);
        }

        return new RectangleF(left, top, 0, 0);
    }

    /// <summary>The box around a rectangle of width x height at (0, 0) after turning it counter-clockwise around (0, 0).</summary>
    private static RectangleF RotatedBounds(float width, float height, float rotation)
    {
        PointF[] corners = { new(0, 0), new(width, 0), new(width, height), new(0, height) };
        using (var turn = new Matrix())
        {
            turn.Rotate(-rotation);
            turn.TransformPoints(corners);
        }

        float left = corners.Min(point => point.X);
        float top = corners.Min(point => point.Y);
        return new RectangleF(left, top, corners.Max(point => point.X) - left, corners.Max(point => point.Y) - top);
    }

    private static (float, float, float, float)? Corners(XElement element, LabelItem label)
    {
        if (Number(element, label, "X") is float x && Number(element, label, "Y") is float y
            && Number(element, label, "X2") is float x2 && Number(element, label, "Y2") is float y2)
        {
            return (x, y, x2, y2);
        }

        return null;
    }

    private static RectangleF Normalised(float x, float y, float x2, float y2)
    {
        return new RectangleF(Math.Min(x, x2), Math.Min(y, y2), Math.Abs(x2 - x), Math.Abs(y2 - y));
    }

    private static float? Number(XElement element, LabelItem label, string name)
    {
        return label.NumberOf(element, name) is double value ? (float)value : null;
    }

    /// <summary>A pen in the thickness of the object's Weight (points, default 1).</summary>
    private static Pen InkPen(XElement element, LabelItem label)
    {
        float weight = Number(element, label, "Weight") ?? 1;
        return new Pen(Theme.LabelInk, Math.Max(0.05f, weight * MillimetresPerPoint));
    }

    /// <summary>"BOLD;ITALIC" (any capitals, also with commas or spaces) to a FontStyle.</summary>
    private static FontStyle ParseFontStyle(string? text)
    {
        FontStyle style = FontStyle.Regular;
        foreach (string word in (text ?? "").Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            style |= word.ToUpperInvariant() switch
            {
                "BOLD" => FontStyle.Bold,
                "ITALIC" => FontStyle.Italic,
                "UNDERLINE" => FontStyle.Underline,
                "STRIKEOUT" => FontStyle.Strikeout,
                _ => FontStyle.Regular,
            };
        }

        return style;
    }

    /// <summary>"The full path name to the image, or relative path to the label-xml."</summary>
    private static string ResolvePath(string path, string? baseFolder)
    {
        if (path == "" || Path.IsPathRooted(path) || baseFolder == null)
        {
            return path;
        }

        return Path.Combine(baseFolder, path);
    }
}

/// <summary>
/// The pictures of the Image objects, read once (the preview is drawn again at every key press). The file is read into
/// memory, so it is never locked. A picture that cannot be read is remembered as missing.
/// </summary>
internal sealed class LabelImages : IDisposable
{
    private readonly Dictionary<string, Image?> _pictures = new(StringComparer.OrdinalIgnoreCase);

    public Image? Get(string path)
    {
        if (path == "")
        {
            return null;
        }

        if (!_pictures.TryGetValue(path, out Image? picture))
        {
            picture = Load(path);
            _pictures[path] = picture;
        }

        return picture;
    }

    /// <summary>Forget all pictures: another file was opened (or a picture may have changed).</summary>
    public void Clear()
    {
        foreach (Image? picture in _pictures.Values)
        {
            picture?.Dispose();
        }

        _pictures.Clear();
    }

    public void Dispose() => Clear();

    private static Image? Load(string path)
    {
        try
        {
            return File.Exists(path) ? Image.FromStream(new MemoryStream(File.ReadAllBytes(path))) : null;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is OutOfMemoryException)
        {
            return null; // OutOfMemoryException is what GDI+ throws for a file that is not a picture
        }
    }
}
