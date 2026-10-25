using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Xml.Linq;

namespace DssLabelTemplate.Tests;

/// <summary>
/// Printing and the codes that must be scannable on paper: a real QR code (QRCoder), barcode bars and QR squares on whole
/// printer dots, which labels the print dialog's choice means, and the page check. A real printer is not used.
/// </summary>
public class PrintingTests
{
    // A 203 dpi label printer (Zebra ZD220): 203 / 25.4 = 7.99 dots per mm, so a millimetre is NOT a whole number of dots.
    private const float DotsPerMm = 203f / 25.4f;

    private static LabelItem Label(params XElement[] objects)
    {
        var printLabel = new XElement("PrintLabel", new XAttribute("ID", "1"), objects);
        return new LabelItem(printLabel, new XElement("DSSPrintJob"), new LabelVariables(null, DateTime.Now));
    }

    /// <summary>Draws a label in black and white on a "printer" of 203 dpi, its (0, 0) a fraction of a dot off the grid.</summary>
    private static Bitmap RenderOnPrinterDots(LabelItem label, bool forPrinter, int widthMm = 60, int heightMm = 60)
    {
        var picture = new Bitmap((int)(widthMm * DotsPerMm), (int)(heightMm * DotsPerMm));
        using Graphics g = Graphics.FromImage(picture);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.None;
        g.TranslateTransform(0.37f, 0.61f);
        g.ScaleTransform(DotsPerMm, DotsPerMm);
        using var images = new LabelImages();
        LabelRenderer.Draw(g, label, baseFolder: null, images, new List<string>(), forPrinter);
        return picture;
    }

    private static bool IsDark(Bitmap picture, int x, int y) => picture.GetPixel(x, y).GetBrightness() < 0.5f;

    /// <summary>The lengths of the runs of equal pixels along one row, from the first dark pixel to the last.</summary>
    private static List<int> RunsAlongRow(Bitmap picture, int y)
    {
        int first = Enumerable.Range(0, picture.Width).First(x => IsDark(picture, x, y));
        int last = Enumerable.Range(0, picture.Width).Last(x => IsDark(picture, x, y));
        var runs = new List<int>();
        int length = 1;
        for (int x = first + 1; x <= last; x++)
        {
            if (IsDark(picture, x, y) == IsDark(picture, x - 1, y))
            {
                length++;
            }
            else
            {
                runs.Add(length);
                length = 1;
            }
        }

        runs.Add(length);
        return runs;
    }

    // ---------------------------------------------------------------- QR code

    [Fact]
    public void TheQrCode_IsARealOne_WithThreeFinderPatterns()
    {
        bool[,] modules = QrCode.Modules("0003575_2.cix")!;

        // A text of 13 characters fits the smallest QR code (version 1, 21 x 21 squares) at level M.
        Assert.Equal(21, modules.GetLength(0));
        Assert.Equal(21, modules.GetLength(1));

        // The three finder patterns: a black 7 x 7 ring, a white ring inside it, a black 3 x 3 centre.
        foreach ((int top, int left) in new[] { (0, 0), (0, 14), (14, 0) })
        {
            Assert.True(modules[top, left] && modules[top + 6, left + 6] && modules[top, left + 6] && modules[top + 6, left]);
            Assert.False(modules[top + 1, left + 1] || modules[top + 5, left + 5]);
            Assert.True(modules[top + 3, left + 3]);
        }

        // Another text gives another code; the same text the same code.
        Assert.NotEqual(Flatten(modules), Flatten(QrCode.Modules("0004544_2.cix")!));
        Assert.Equal(Flatten(modules), Flatten(QrCode.Modules("0003575_2.cix")!));
    }

    [Fact]
    public void TooMuchTextForAQrCode_IsAnObjectThatIsNotDrawn()
    {
        string tooLong = new('x', 5000); // more than the biggest QR code (version 40) holds
        Assert.Null(QrCode.Modules(tooLong));

        var problems = new List<string>();
        using var picture = new Bitmap(100, 100);
        using Graphics g = Graphics.FromImage(picture);
        using var images = new LabelImages();
        var qr = new XElement("QRCode", new XAttribute("X", "1"), new XAttribute("Y", "1"), new XAttribute("Width", "10"), new XAttribute("Height", "10"), tooLong);
        LabelRenderer.Draw(g, Label(qr), null, images, problems);

        Assert.Single(problems);
    }

    [Fact]
    public void TheDrawnQrCode_HasExactlyTheSquaresOfTheCode()
    {
        const string text = "0003575_2.cix";
        bool[,] modules = QrCode.Modules(text)!;
        var qr = new XElement("QRCode", new XAttribute("X", "5"), new XAttribute("Y", "5"), new XAttribute("Width", "21"), new XAttribute("Height", "30"), text);

        // 21 mm for 21 squares: 1 mm per square (the smaller of Width and Height); 10 pixels per mm on this picture.
        using var picture = new Bitmap(300, 400);
        using (Graphics g = Graphics.FromImage(picture))
        {
            g.Clear(Color.White);
            g.ScaleTransform(10, 10);
            using var images = new LabelImages();
            LabelRenderer.Draw(g, Label(qr), null, images, new List<string>());
        }

        for (int row = 0; row < 21; row++)
        {
            for (int column = 0; column < 21; column++)
            {
                int x = 50 + column * 10 + 5, y = 50 + row * 10 + 5; // the middle of the square
                Assert.True(modules[row, column] == IsDark(picture, x, y), $"square {row}, {column}");
            }
        }
    }

    private static string Flatten(bool[,] modules) => string.Concat(modules.Cast<bool>().Select(dark => dark ? '1' : '0'));

    // ---------------------------------------------------------------- whole printer dots

    [Fact]
    public void OnThePrinter_EveryBarOfABarcode_IsAWholeNumberOfModules()
    {
        // 40 mm for the Code 128 of "0003575_2.cix" (13 characters: 13 x 11 + 35 = 178 modules) = 1.8 dots per module
        // at 203 dpi: on the printer every module must become a whole number of dots (here 1), all exactly as wide.
        var barcode = new XElement("Barcode", new XAttribute("X", "5"), new XAttribute("Y", "5"), new XAttribute("Width", "40"),
            new XAttribute("Height", "10"), new XAttribute("Type", "BARCODE128"), "0003575_2.cix");
        bool[] modules = Code128.Modules("0003575_2.cix");
        int dotsPerModule = (int)Math.Floor(40 * DotsPerMm / modules.Length);

        using Bitmap printed = RenderOnPrinterDots(Label(barcode), forPrinter: true);
        List<int> runs = RunsAlongRow(printed, (int)(10 * DotsPerMm));

        // Every bar and space is 1 – 4 modules, each module exactly dotsPerModule dots; the whole code is modules x that.
        Assert.All(runs, run => Assert.True(run % dotsPerModule == 0 && run / dotsPerModule is >= 1 and <= 4, "run of " + run + " dots"));
        Assert.Equal(modules.Length * dotsPerModule, runs.Sum());
        Assert.True(runs.Sum() <= 40 * DotsPerMm, "never wider than the Width of the template");
    }

    [Fact]
    public void OnThePrinter_EverySquareOfAQrCode_IsTheSameWholeNumberOfDots()
    {
        // 8 mm for 21 squares (the 60 x 60 template of HUPPY-PINCKAERS) = 3.04 dots per square at 203 dpi: 3 dots each.
        var qr = new XElement("QRCode", new XAttribute("X", "44"), new XAttribute("Y", "44"), new XAttribute("Width", "8"),
            new XAttribute("Height", "8"), new XAttribute("Type", "AUTO"), "0003575_2.cix");

        using Bitmap printed = RenderOnPrinterDots(Label(qr), forPrinter: true);
        int middleOfFirstRow = (int)(44 * DotsPerMm) + 2; // inside the top row of squares (the finder patterns)
        List<int> runs = RunsAlongRow(printed, middleOfFirstRow);

        Assert.All(runs, run => Assert.True(run % 3 == 0, "run of " + run + " dots"));
        Assert.Equal(21 * 3, runs.Sum());
    }

    [Fact]
    public void TheHardMarginOfThePrinter_IsTakenOff_SoTheLabelStartsAtTheCornerOfThePaper()
    {
        // A 10 mm square at (0, 0) of the label. A printer starts drawing at the corner of what it can print (the hard
        // margin, here 5 mm from the left): the page must be moved back, so only the right half of the square is printed.
        var square = new XElement("Rectangle", new XAttribute("X", "0"), new XAttribute("Y", "0"), new XAttribute("X2", "10"), new XAttribute("Y2", "10"), new XAttribute("Weight", "1"));
        using var picture = new Bitmap(400, 400);
        picture.SetResolution(203, 203); // PageUnit Millimeter uses the resolution of the picture
        using (Graphics g = Graphics.FromImage(picture))
        {
            g.Clear(Color.White);
            using var images = new LabelImages();
            LabelPrinter.DrawPage(g, new PointF(5, 0), Label(square), null, images);
        }

        int y = (int)(5 * DotsPerMm); // halfway down the square: only its right side is on the page
        int lastDark = Enumerable.Range(0, picture.Width).Last(x => IsDark(picture, x, y));
        Assert.InRange(lastDark, (int)(4.5 * DotsPerMm), (int)(5.5 * DotsPerMm));
    }

    // ---------------------------------------------------------------- the print dialog's choice and the page check

    [Fact]
    public void TheLabelsToPrint_FollowTheChoiceInThePrintDialog()
    {
        Assert.Equal(new[] { 1 }, LabelPrinter.LabelsToPrint(PrintRange.CurrentPage, 1, 1, shown: 1, count: 3));
        Assert.Equal(new[] { 0, 1, 2 }, LabelPrinter.LabelsToPrint(PrintRange.AllPages, 1, 1, shown: 1, count: 3));
        Assert.Equal(new[] { 1, 2 }, LabelPrinter.LabelsToPrint(PrintRange.SomePages, 2, 3, shown: 0, count: 3));
        Assert.Equal(new[] { 1, 2 }, LabelPrinter.LabelsToPrint(PrintRange.SomePages, 3, 2, shown: 0, count: 3)); // typed the other way round
        Assert.Equal(new[] { 2 }, LabelPrinter.LabelsToPrint(PrintRange.SomePages, 3, 9, shown: 0, count: 3));    // past the last label
        Assert.Empty(LabelPrinter.LabelsToPrint(PrintRange.SomePages, 5, 9, shown: 0, count: 3));
        Assert.Empty(LabelPrinter.LabelsToPrint(PrintRange.CurrentPage, 1, 1, shown: 0, count: 0));
    }

    [Fact]
    public void ThePaperFits_WhenItIsTheLabelSizeWithinOneMillimetre()
    {
        var label = new SizeF(100, 100);

        Assert.True(LabelPrinter.PaperFits(new SizeF(100, 100), label));
        Assert.True(LabelPrinter.PaperFits(new SizeF(100.08f, 99.82f), label)); // .NET rounds page sizes to 1/100 inch
        Assert.False(LabelPrinter.PaperFits(new SizeF(101.6f, 101.6f), label)); // 4 x 4 inch: 1.6 mm too big
        Assert.False(LabelPrinter.PaperFits(new SizeF(60, 60), label));
        Assert.False(LabelPrinter.PaperFits(new SizeF(210, 297), label)); // an A4 printer chosen by mistake
    }
}
