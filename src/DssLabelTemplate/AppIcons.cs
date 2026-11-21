using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace DssLabelTemplate;

/// <summary>Loads the icons and draws the banner from the pictures that are built into the exe (from the media folder).</summary>
internal static class AppIcons
{
    // The banner is drawn at twice its size at 100% (1080 x 168 = 2 x 540 x 84, like TsDssConverter's header@2x.png) and
    // scaled down by the window, so it stays sharp on high-DPI screens. Left the logo, right of it the name and one line.
    private const int BannerPictureWidth = 1080;
    private const int BannerPictureHeight = 168;
    private static readonly RectangleF BannerLogo = new(26, 14, 140, 140);
    private const int BannerTextLeft = 194;      // the title starts where TsDssConverter's does

    /// <summary>The icon for the window (title bar, taskbar): all sizes, Windows picks the right one.</summary>
    public static Icon ForWindow()
    {
        using var stream = OpenResource("app.ico");
        return new Icon(stream);
    }

    /// <summary>
    /// The banner (only for the README since 2.0.0): the logo (<see cref="AppLogo"/>) on the blue of the Maestro skin's
    /// bottom bar (Daan, 2026-11-21; it was the brand colour), with the name of the program and one line below it, in
    /// white like the counter on that bar. The text is in English in every language (like TsDssConverter's banner).
    /// </summary>
    public static Image Banner()
    {
        var banner = new Bitmap(BannerPictureWidth, BannerPictureHeight);
        using var graphics = Graphics.FromImage(banner);
        var all = new Rectangle(0, 0, BannerPictureWidth, BannerPictureHeight);
        using (var bar = new LinearGradientBrush(all, Theme.BarTop, Theme.BarBottom, LinearGradientMode.Vertical))
        {
            graphics.FillRectangle(bar, all);
        }
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        AppLogo.Draw(graphics, BannerLogo, withBackground: false);

        // The same places as the text of TsDssConverter's banner: the title's baseline at y = 82, the subtitle's at y = 128.
        using var titleFont = Theme.CreateBannerTitleFont(54);
        using var subtitleFont = Theme.CreateBannerSubtitleFont(27);
        using var white = new SolidBrush(Theme.White);
        DrawOnBaseline(graphics, Strings.AppName, titleFont, white, BannerTextLeft, 82);
        DrawOnBaseline(graphics, Strings.BannerSubtitle, subtitleFont, white, BannerTextLeft + 2, 128);
        return banner;
    }

    /// <summary>Draws a text so that the bottom of its letters (the baseline) is on the given y.</summary>
    private static void DrawOnBaseline(Graphics graphics, string text, Font font, Brush brush, float left, float baseline)
    {
        FontFamily family = font.FontFamily;
        float ascent = font.Size * family.GetCellAscent(font.Style) / family.GetEmHeight(font.Style);
        graphics.DrawString(text, font, brush, left, baseline - ascent, StringFormat.GenericTypographic);
    }

    /// <summary>
    /// The small logo of ROGIERS at the bottom left of the window. The picture file has a transparent margin around the logo; that is
    /// cut off, so the logo itself lines up with the other controls. It is made smaller once, with high quality:
    /// the window then only scales a small picture (a big picture scaled down by a PictureBox looks jagged).
    /// </summary>
    public static Image CompanyLogo()
    {
        return TrimmedAndScaled("rogiers-logo.png", 128); // enough for a screen at 250%: shown about 44 pixels wide at 100%
    }

    /// <summary>
    /// The small "user guide" icon (a book with a question mark) next to the app version. Made smaller once like the
    /// company logo: shown 16 pixels high at 100%, so 64 is enough for a screen at 400%.
    /// </summary>
    public static Image UserGuide()
    {
        return TrimmedAndScaled("user-guide.png", 64);
    }

    /// <summary>
    /// The printer icon on the print button next to the arrows (media\Print.png, navy on transparent). Made smaller once
    /// like the other icons: shown 18 pixels high at 100%, so 72 is enough for a screen at 400%.
    /// </summary>
    public static Image Print()
    {
        return TrimmedAndScaled("print.png", 72);
    }

    /// <summary>
    /// The printer in WHITE, for the navy circle of the print button in the bottom bar (2.0.0): the same picture as
    /// <see cref="Print"/>, every visible pixel made white (its transparency is kept).
    /// </summary>
    public static Image PrintWhite()
    {
        using Image navy = Print();
        return InColour(navy, Theme.White);
    }

    /// <summary>
    /// A folder (a white outline), drawn in code, before the text of the Browse buttons (2.0.0). Drawn at 64 pixels wide:
    /// shown 18 high at 100%, so sharp up to about 300%.
    /// </summary>
    public static Image Folder()
    {
        // The folder of the mockup, on a grid of 24: a tab at the top left, then the body.
        const float grid = 24, size = 64, scale = size / grid;
        var folder = new Bitmap((int)size, (int)(14 * scale) + 8);
        using var graphics = Graphics.FromImage(folder);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Theme.White, 2.2f * scale) { LineJoin = LineJoin.Round };
        float top = 4; // room for half the pen width
        PointF[] outline =
        {
            new(2 * scale, top), new(8 * scale, top), new(10 * scale, top + 2 * scale), new(22 * scale, top + 2 * scale),
            new(22 * scale, top + 14 * scale), new(2 * scale, top + 14 * scale),
        };
        graphics.DrawPolygon(pen, outline);
        return folder;
    }

    /// <summary>A copy of a picture in one colour: every pixel gets <paramref name="colour"/>, its transparency stays.</summary>
    private static Image InColour(Image picture, Color colour)
    {
        var copy = new Bitmap(picture.Width, picture.Height);
        using var graphics = Graphics.FromImage(copy);
        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new[]
        {
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { colour.R / 255f, colour.G / 255f, colour.B / 255f, 0, 1 },
        }));
        graphics.DrawImage(picture, new Rectangle(0, 0, picture.Width, picture.Height), 0, 0, picture.Width, picture.Height, GraphicsUnit.Pixel, attributes);
        return copy;
    }

    /// <summary>
    /// The board icon on the "View board" button left of the arrows (media\Board.png, navy on transparent). Shown as high
    /// as the printer icon, so 72 is enough for a screen at 400%.
    /// </summary>
    public static Image Board()
    {
        return TrimmedAndScaled("board.png", 72);
    }

    /// <summary>
    /// The csv icon (media\CSV.png, a navy tag with "CSV" on transparent) on the button left of the board button, which
    /// shows the csv table. Shown as high as the board icon; wider than high, so 144 on its long side for a screen at 400%.
    /// </summary>
    public static Image Csv()
    {
        return TrimmedAndScaled("csv.png", 144);
    }

    /// <summary>
    /// The "?" icon before the two path captions (media\howto.png, a navy circle with a white question mark): its hover
    /// text describes the two kinds of label files. Shown 16 pixels high at 100%, so 64 is enough for a screen at 400%.
    /// </summary>
    public static Image HowTo()
    {
        return TrimmedAndScaled("howto.png", 64);
    }

    /// <summary>A built-in picture without its transparent margin, made smaller once (high quality) to the given long side.</summary>
    private static Image TrimmedAndScaled(string resourceName, int longSide)
    {
        using var stream = OpenResource(resourceName);
        using var original = new Bitmap(stream);
        Rectangle visible = FindVisibleArea(original);

        double scale = (double)longSide / Math.Max(visible.Width, visible.Height);
        var logo = new Bitmap((int)Math.Round(visible.Width * scale), (int)Math.Round(visible.Height * scale));

        using var graphics = Graphics.FromImage(logo);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(original, new Rectangle(Point.Empty, logo.Size), visible, GraphicsUnit.Pixel);
        return logo;
    }

    /// <summary>The smallest rectangle that holds every pixel that is not (almost) fully transparent.</summary>
    private static Rectangle FindVisibleArea(Bitmap picture)
    {
        var all = new Rectangle(0, 0, picture.Width, picture.Height);
        var data = picture.LockBits(all, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            int left = picture.Width, top = picture.Height, right = -1, bottom = -1;
            var row = new byte[data.Stride];

            for (int y = 0; y < picture.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Stride);

                for (int x = 0; x < picture.Width; x++)
                {
                    bool visiblePixel = row[x * 4 + 3] > 16; // byte 3 of a pixel (B, G, R, A) is the alpha: 0 = transparent
                    if (visiblePixel)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }
                }
            }

            // A picture without any visible pixel: use it whole.
            return right < 0 ? all : new Rectangle(left, top, right - left + 1, bottom - top + 1);
        }
        finally
        {
            picture.UnlockBits(data);
        }
    }

    private static Stream OpenResource(string name)
    {
        return typeof(AppIcons).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Built-in file not found: " + name);
    }
}
