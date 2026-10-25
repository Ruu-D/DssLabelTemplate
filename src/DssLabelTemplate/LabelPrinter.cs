using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;

namespace DssLabelTemplate;

/// <summary>
/// Prints labels on an ordinary Windows printer, one label per page: meant for a label printer next to the unloading
/// belt (a Zebra ZD220, USB, direct thermal, peel-off) to REPRINT a damaged label. The automatic printing on the label
/// station stays the job of DSSPrintServer.
///
/// The label is drawn by the same <see cref="LabelRenderer"/> as the preview, 1 to 1 in millimetres from the top left
/// corner of the paper. The page size itself comes from the printer driver: on the reprint PC the driver is set up with
/// the size of the label (Daan, 2026-10-25). Before printing, the window compares that page size with the label size.
/// </summary>
internal static class LabelPrinter
{
    private const float MillimetresPerHundredthInch = 0.254f; // .NET gives page sizes and margins in 1/100 inch

    /// <summary>How much the page of the printer may differ from the label size (mm) before the window asks.</summary>
    public const float PaperTolerance = 1f;

    /// <summary>
    /// The labels (0 = the first) to print for what was chosen in the print dialog: all labels, the "pages" from – to
    /// (page n = label n, as in "2 / 3"), or the label that is shown now ("current page", the default).
    /// </summary>
    public static List<int> LabelsToPrint(PrintRange range, int fromPage, int toPage, int shown, int count)
    {
        switch (range)
        {
            case PrintRange.AllPages:
                return Enumerable.Range(0, count).ToList();

            case PrintRange.SomePages:
                int first = Math.Max(1, Math.Min(fromPage, toPage));
                int last = Math.Min(count, Math.Max(fromPage, toPage));
                return first > last ? new List<int>() : Enumerable.Range(first - 1, last - first + 1).ToList();

            default:
                return shown >= 0 && shown < count ? new List<int> { shown } : new List<int>();
        }
    }

    /// <summary>The size of the printer's page in mm (length across, width down), as the driver is set up now.</summary>
    public static SizeF PaperSize(PageSettings page)
    {
        Rectangle bounds = page.Bounds; // already turned for landscape
        return new SizeF(bounds.Width * MillimetresPerHundredthInch, bounds.Height * MillimetresPerHundredthInch);
    }

    /// <summary>True when the page is the label size (within <see cref="PaperTolerance"/>).</summary>
    public static bool PaperFits(SizeF paper, SizeF label)
    {
        return Math.Abs(paper.Width - label.Width) <= PaperTolerance && Math.Abs(paper.Height - label.Height) <= PaperTolerance;
    }

    /// <summary>
    /// Prints the labels, one per page, on the printer of the document (chosen in the print dialog). May throw
    /// InvalidPrinterException or Win32Exception (printer gone, spooler stopped); the window reports it.
    /// </summary>
    public static void Print(PrintDocument document, IReadOnlyList<LabelItem> labels, string? baseFolder)
    {
        if (labels.Count == 0)
        {
            return;
        }

        using var images = new LabelImages(); // read again: a picture may have changed since the preview read it
        int next = 0;
        void PrintPage(object? sender, PrintPageEventArgs e)
        {
            var hardMargin = new PointF(e.PageSettings.HardMarginX * MillimetresPerHundredthInch, e.PageSettings.HardMarginY * MillimetresPerHundredthInch);
            DrawPage(e.Graphics!, hardMargin, labels[next], baseFolder, images);
            next++;
            e.HasMorePages = next < labels.Count;
        }

        document.PrintPage += PrintPage;
        try
        {
            document.Print();
        }
        finally
        {
            document.PrintPage -= PrintPage;
        }
    }

    /// <summary>
    /// Draws one label on a page: 1 unit = 1 mm, (0, 0) = the top left corner of the PAPER. A printer's drawing starts at
    /// the corner of the area it can print on (the hard margin), so it is moved back by that margin.
    /// </summary>
    internal static void DrawPage(Graphics g, PointF hardMarginMillimetres, LabelItem label, string? baseFolder, LabelImages images)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        g.TranslateTransform(-hardMarginMillimetres.X, -hardMarginMillimetres.Y);

        // A thermal label printer only has black and white dots: no grey (anti-aliased) edges, they would come out as
        // a ragged pattern of dots.
        g.SmoothingMode = SmoothingMode.None;
        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

        LabelRenderer.Draw(g, label, baseFolder, images, new List<string>(), forPrinter: true);
    }
}
