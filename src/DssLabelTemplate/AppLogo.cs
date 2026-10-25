using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DssLabelTemplate;

/// <summary>
/// The logo of the program, drawn in code: a white label (sticker) with a QR-code-like pattern and a peeled-off bottom right
/// corner, with a dark blue shadow. On the app icon it sits on a rounded square in the brand colour; in the banner it sits
/// on the banner's own brand colour. Only colours of the theme. The same drawing makes the banner AND media\app.ico
/// (see <see cref="IconFileWriter"/>), so the two always match.
/// </summary>
internal static class AppLogo
{
    // The drawing is made on a grid of 100 x 100 units and scaled to the box it is drawn in.
    private const float LabelLeft = 16, LabelTop = 16, LabelSize = 68, LabelCorner = 9;
    private const float PeelSize = 18;          // the peeled-off corner, from the bottom right corner of the label
    private const float ShadowOffset = 3;
    private const float PatternLeft = 22, PatternSize = 56, PatternCells = 9; // the QR-like pattern: 9 x 9 cells

    // The cells of the pattern that are dark, besides the three corner squares (column, row). Free of the peeled corner.
    private static readonly (int Column, int Row)[] DataCells =
    {
        (4, 0), (3, 1), (5, 2), (0, 3), (1, 4), (2, 5), (3, 3), (4, 4), (5, 3), (3, 5),
        (5, 5), (6, 3), (7, 4), (8, 3), (6, 5), (4, 6), (3, 7), (5, 7), (4, 8), (6, 7),
    };

    /// <param name="withBackground">True for the app icon (brand blue square behind the label); false on the banner.</param>
    public static void Draw(Graphics g, RectangleF box, bool withBackground)
    {
        GraphicsState state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TranslateTransform(box.X, box.Y);
        g.ScaleTransform(box.Width / 100f, box.Height / 100f);

        // Small icons (16, 24 px) only get the label, the three corner squares and the peeled corner: details would blur.
        bool detailed = box.Width >= 30;

        if (withBackground)
        {
            using GraphicsPath square = FluentDrawing.RoundedRectangle(new RectangleF(2, 2, 96, 96), 18);
            using var brand = new SolidBrush(Theme.Brand);
            g.FillPath(brand, square);
        }

        using (GraphicsPath shadow = LabelOutline(LabelLeft + ShadowOffset, LabelTop + ShadowOffset))
        using (var darkBlue = new SolidBrush(Theme.DarkBlue))
        {
            g.FillPath(darkBlue, shadow);
        }

        using (GraphicsPath label = LabelOutline(LabelLeft, LabelTop))
        using (var white = new SolidBrush(Theme.White))
        {
            g.FillPath(white, label);
        }

        // The peeled corner: the cut-off corner folded back over the label (its pale blue back side).
        float right = LabelLeft + LabelSize, bottom = LabelTop + LabelSize;
        PointF[] flap = { new(right, bottom - PeelSize), new(right - PeelSize, bottom), new(right - PeelSize, bottom - PeelSize) };
        using (var pale = new SolidBrush(Theme.PaleBlue))
        {
            g.FillPolygon(pale, flap);
        }

        float cell = PatternSize / PatternCells;
        using var navy = new SolidBrush(Theme.Navy);
        using var paper = new SolidBrush(Theme.White);
        foreach ((int column, int row) in new[] { (0, 0), (6, 0), (0, 6) })
        {
            float x = PatternLeft + column * cell, y = PatternLeft + row * cell;
            g.FillRectangle(navy, x, y, 3 * cell, 3 * cell);
            g.FillRectangle(paper, x + cell * 0.5f, y + cell * 0.5f, cell * 2, cell * 2);
            g.FillRectangle(navy, x + cell, y + cell, cell, cell);
        }

        if (detailed)
        {
            float gap = cell * 0.08f;
            foreach ((int column, int row) in DataCells)
            {
                g.FillRectangle(navy, PatternLeft + column * cell + gap, PatternLeft + row * cell + gap, cell - 2 * gap, cell - 2 * gap);
            }
        }

        g.Restore(state);
    }

    /// <summary>The label: a rounded square whose bottom right corner is cut off (where it is peeled).</summary>
    private static GraphicsPath LabelOutline(float left, float top)
    {
        float right = left + LabelSize, bottom = top + LabelSize, d = LabelCorner * 2;
        var path = new GraphicsPath();
        path.AddArc(left, top, d, d, 180, 90);
        path.AddArc(right - d, top, d, d, 270, 90);
        path.AddLine(right, top + LabelCorner, right, bottom - PeelSize);
        path.AddLine(right, bottom - PeelSize, right - PeelSize, bottom);
        path.AddLine(right - PeelSize, bottom, left + LabelCorner, bottom);
        path.AddArc(left, bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>The logo with its background as a picture of size x size pixels (transparent outside the square).</summary>
    public static Bitmap Render(int size)
    {
        var picture = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(picture);
        g.Clear(Color.Transparent);
        Draw(g, new RectangleF(0, 0, size, size), withBackground: true);
        return picture;
    }
}

/// <summary>
/// Writes media\app.ico (and the big PNG versions) from <see cref="AppLogo"/>. Run it after changing the logo:
///   dotnet run --project src/DssLabelTemplate -- --write-icon media
/// then build again (app.ico is built into the exe).
/// </summary>
internal static class IconFileWriter
{
    /// <summary>The sizes Windows uses for the exe, title bar, taskbar and Explorer at 100% – 400%.</summary>
    public static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };

    public static void WriteAll(string folder)
    {
        Directory.CreateDirectory(folder);
        WriteIco(Path.Combine(folder, "app.ico"));

        foreach (int size in new[] { 256, 512 })
        {
            using Bitmap picture = AppLogo.Render(size);
            picture.Save(Path.Combine(folder, $"icon-{size}.png"), ImageFormat.Png);
        }
    }

    /// <summary>
    /// The .ico format: a small header, one directory entry per size, then the pictures. 256 is stored as PNG (as Windows
    /// does); the smaller sizes as a 32-bit bitmap with alpha plus the old 1-bit transparency mask, which every program reads.
    /// </summary>
    public static void WriteIco(string path)
    {
        var images = Sizes.Select(size => size >= 256 ? PngBytes(size) : BitmapBytes(size)).ToList();

        using var file = new BinaryWriter(File.Create(path));
        file.Write((short)0);            // reserved
        file.Write((short)1);            // 1 = icon
        file.Write((short)Sizes.Length);

        int offset = 6 + 16 * Sizes.Length;
        for (int i = 0; i < Sizes.Length; i++)
        {
            file.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i])); // width (0 = 256)
            file.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i])); // height
            file.Write((byte)0);         // no palette
            file.Write((byte)0);         // reserved
            file.Write((short)1);        // planes
            file.Write((short)32);       // bits per pixel
            file.Write(images[i].Length);
            file.Write(offset);
            offset += images[i].Length;
        }

        foreach (byte[] image in images)
        {
            file.Write(image);
        }
    }

    private static byte[] PngBytes(int size)
    {
        using Bitmap picture = AppLogo.Render(size);
        using var memory = new MemoryStream();
        picture.Save(memory, ImageFormat.Png);
        return memory.ToArray();
    }

    private static byte[] BitmapBytes(int size)
    {
        using Bitmap picture = AppLogo.Render(size);
        var data = picture.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var pixels = new byte[data.Stride * size];
        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        picture.UnlockBits(data);

        int maskStride = (size + 31) / 32 * 4; // one bit per pixel, every row a multiple of 4 bytes
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);

        // BITMAPINFOHEADER. The height counts twice: the colour picture and the mask below it.
        writer.Write(40);
        writer.Write(size);
        writer.Write(size * 2);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(0);                 // no compression
        writer.Write(size * size * 4 + maskStride * size);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        // The pixels (blue, green, red, alpha), bottom row first.
        for (int y = size - 1; y >= 0; y--)
        {
            writer.Write(pixels, y * data.Stride, size * 4);
        }

        // The mask: 1 = transparent (alpha 0), also bottom row first.
        for (int y = size - 1; y >= 0; y--)
        {
            var row = new byte[maskStride];
            for (int x = 0; x < size; x++)
            {
                if (pixels[y * data.Stride + x * 4 + 3] == 0)
                {
                    row[x / 8] |= (byte)(0x80 >> (x % 8));
                }
            }

            writer.Write(row);
        }

        return memory.ToArray();
    }
}
