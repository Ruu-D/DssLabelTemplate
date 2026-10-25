using System.Drawing.Drawing2D;
using System.Xml.Linq;

namespace DssLabelTemplate.Tests;

/// <summary>Drawing the labels of the manual: everything can be drawn, and something is really drawn.</summary>
public class LabelRendererTests
{
    private const float PixelsPerMm = 4;
    private static readonly SizeF DefaultSize = new((float)AppSettings.DefaultLabelSize, (float)AppSettings.DefaultLabelSize);

    /// <summary>Draws one label like the preview does and returns the picture and the problems.</summary>
    private static (Bitmap Picture, List<string> Problems) Render(LabelItem label)
    {
        SizeF size = DefaultSize;
        var picture = new Bitmap((int)(size.Width * PixelsPerMm), (int)(size.Height * PixelsPerMm));
        var problems = new List<string>();

        using Graphics g = Graphics.FromImage(picture);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(PixelsPerMm, PixelsPerMm);
        using var images = new LabelImages();
        LabelRenderer.Draw(g, label, baseFolder: null, images, problems);
        return (picture, problems);
    }

    private static int DarkPixels(Bitmap picture)
    {
        int dark = 0;
        for (int y = 0; y < picture.Height; y += 2)
        {
            for (int x = 0; x < picture.Width; x += 2)
            {
                if (picture.GetPixel(x, y).GetBrightness() < 0.5f)
                {
                    dark++;
                }
            }
        }

        return dark;
    }

    /// <summary>The 3 labels of the template + csv and the 2 of the label-xml of the manual.</summary>
    private static List<LabelItem> AllLabelsOfTheManual()
    {
        var now = new DateTime(2026, 10, 24);
        LabelSet template = LabelSetBuilder.Build(Samples.Text(Samples.Template), LabelCsv.Read(Samples.File(Samples.Csv)), now);
        LabelSet labelXml = LabelSetBuilder.Build(Samples.Text(Samples.LabelXml), null, now);
        return template.Labels.Concat(labelXml.Labels).ToList();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void EveryLabelOfTheManual_IsDrawnWithoutProblems(int index)
    {
        LabelItem label = AllLabelsOfTheManual()[index];

        (Bitmap picture, List<string> problems) = Render(label);
        using (picture)
        {
            Assert.True(problems.Count == 0, $"label {label.Id}: {string.Join(", ", problems)}");
            Assert.True(DarkPixels(picture) > 200, $"label {label.Id}: hardly anything was drawn");
        }
    }

    [Fact]
    public void TheLabelsOfTheManual_FitInTheDefaultSize_ButNotInASmallerOne()
    {
        foreach (LabelItem label in AllLabelsOfTheManual())
        {
            List<RectangleF> objects = LabelRenderer.ObjectBounds(label);
            Assert.Equal(0, LabelRenderer.CountOutside(objects, DefaultSize));
            Assert.Equal(new RectangleF(PointF.Empty, DefaultSize), LabelRenderer.ViewArea(objects, DefaultSize));
            Assert.True(LabelRenderer.CountOutside(objects, new SizeF(60, 40)) > 0, $"label {label.Id} on a 60 x 40 label");
        }
    }

    [Fact]
    public void AnObjectThatSticksOut_IsCounted_AndTheViewGrowsToShowIt()
    {
        LabelItem label = Label(
            new XElement("Rectangle", new XAttribute("X", "10"), new XAttribute("Y", "10"), new XAttribute("X2", "120.4"), new XAttribute("Y2", "50")),
            new XElement("Line", new XAttribute("X", "0"), new XAttribute("Y", "100"), new XAttribute("X2", "100"), new XAttribute("Y2", "100")));
        List<RectangleF> objects = LabelRenderer.ObjectBounds(label);

        Assert.Equal(1, LabelRenderer.CountOutside(objects, DefaultSize)); // the line on the edge is not outside
        Assert.Equal(120.4f, LabelRenderer.ViewArea(objects, DefaultSize).Right, 3);
        Assert.Equal(0, LabelRenderer.CountOutside(objects, new SizeF(150, 100)));
    }

    [Fact]
    public void AnObjectWithAWrongNumber_IsNamedWithItsLine_AndTheRestIsDrawn()
    {
        string xml = "<DSSPrintJob Length=\"2800\" Width=\"2100\">\n" +
                     "  <PrintLabel ID=\"1\" X=\"100\" Y=\"100\">\n" +
                     "    <Text Y=\"5\">no X</Text>\n" +
                     "    <Line X=\"0\" Y=\"50\" X2=\"100\" Y2=\"50\" Weight=\"8\"/>\n" +
                     "  </PrintLabel>\n" +
                     "</DSSPrintJob>";
        LabelItem label = LabelSetBuilder.Build(xml, null, DateTime.Now).Labels[0];

        (Bitmap picture, List<string> problems) = Render(label);
        using (picture)
        {
            Assert.Equal(Strings.ObjectNotDrawn("Text", 3), Assert.Single(problems));
            Assert.True(DarkPixels(picture) > 50, "the line was drawn");
        }
    }

    [Fact]
    public void UnknownObjects_AreSkippedSilently()
    {
        (Bitmap picture, List<string> problems) = Render(Label(new XElement("Hologram", new XAttribute("X", "1"))));
        picture.Dispose();

        Assert.Empty(problems);
    }

    [Fact]
    public void QrCodeAndAllArrowDirectionsAndFills_CanBeDrawn()
    {
        var objects = new List<XElement>
        {
            new("QRCode", new XAttribute("X", "5"), new XAttribute("Y", "5"), new XAttribute("Width", "30"), new XAttribute("Height", "30"), "ABC"),
        };
        for (int direction = 1; direction <= 8; direction++)
        {
            objects.Add(new XElement("Arrow", new XAttribute("X", 10 * direction), new XAttribute("Y", "50"),
                new XAttribute("X2", 10 * direction + 8), new XAttribute("Y2", "58"),
                new XAttribute("Direction", direction), new XAttribute("FillStyle", direction - 1)));
        }

        (Bitmap picture, List<string> problems) = Render(Label(objects.ToArray()));
        using (picture)
        {
            Assert.Empty(problems);
            Assert.True(DarkPixels(picture) > 200);
        }
    }

    private static LabelItem Label(params XElement[] objects)
    {
        var printLabel = new XElement("PrintLabel", new XAttribute("ID", "1"), objects);
        return new LabelItem(printLabel, new XElement("DSSPrintJob"), new LabelVariables(null, DateTime.Now));
    }
}
