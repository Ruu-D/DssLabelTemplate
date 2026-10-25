using System.Xml.Linq;

namespace DssLabelTemplate.Tests;

/// <summary>Reading the two kinds of label files of the manual: label-csv + template, and label-xml.</summary>
public class LabelSetTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 14, 32, 5);

    private static LabelSet TemplateWithCsv() =>
        LabelSetBuilder.Build(Samples.Text(Samples.Template), LabelCsv.Read(Samples.File(Samples.Csv)), Now);

    [Fact]
    public void TheTemplateOfTheManual_WithItsCsv_GivesOneLabelPerCsvLine()
    {
        LabelSet set = TemplateWithCsv();

        Assert.Null(set.Error);
        Assert.Equal(LabelFileKind.Template, set.Kind);
        Assert.Equal(new[] { "931", "933", "932" }, set.Labels.Select(label => label.Id));
        Assert.Equal(1145, set.Labels[0].X);
        Assert.Equal(1483, set.Labels[2].Y);
        Assert.Equal(90, set.Labels[1].Rotation);
        Assert.Equal(2820, set.Labels[0].SheetLength);
        Assert.Equal(2070, set.Labels[0].SheetWidth);
    }

    [Fact]
    public void TheVariablesOfATemplate_AreFilledInFromTheCsvLine()
    {
        LabelItem label = TemplateWithCsv().Labels[2];
        List<string> texts = label.Element.Elements().Where(e => LabelXml.Is(e, "Text"))
            .Select(e => label.Variables.Resolve(e.Value)).ToList();

        Assert.Contains("3629310", texts); // $ORDERNO$
        Assert.Contains("788", texts);     // $PW$
        Assert.Contains("350.2", texts);   // $PH$
    }

    [Fact]
    public void TheLabelXmlOfTheManual_HasTwoLabels_WithTheirPlaceOnTheSheet()
    {
        LabelSet set = LabelSetBuilder.Build(Samples.Text(Samples.LabelXml), csv: null, Now);

        Assert.Null(set.Error);
        Assert.Equal(LabelFileKind.LabelXml, set.Kind);
        Assert.Equal(new[] { "5436280", "5436281" }, set.Labels.Select(label => label.Id));
        Assert.Equal(650, set.Labels[1].X);
        Assert.Equal(300, set.Labels[1].Y);
        Assert.Equal(0, set.Labels[1].Rotation); // no ROTATION attribute: 0
        Assert.Equal(2800, set.Labels[0].SheetLength);
    }

    [Fact]
    public void DateAndTime_AreFilledInWhenShown()
    {
        var variables = new LabelVariables(row: null, Now);

        Assert.Equal("24-10-2026 / 14:32:05", variables.Resolve("$DATE$ / $TIME$"));
        Assert.Equal("$UNKNOWN$", variables.Resolve("$UNKNOWN$")); // an unknown variable stays visible
    }

    [Fact]
    public void ATemplateWithoutCsv_IsOneLabelWithItsVariablesStillVisible()
    {
        LabelSet set = LabelSetBuilder.Build(Samples.Text(Samples.Template), csv: null, Now);

        Assert.Equal(LabelFileKind.Template, set.Kind);
        Assert.Equal("$ID$", Assert.Single(set.Labels).Id);
        Assert.Null(set.Labels[0].X);
    }

    [Fact]
    public void TheTemplateOfThePdfWithItsTypo_IsReportedAsAnXmlError()
    {
        // The PDF writes Width="$WIDTH$"" (two quotes): that is not xml. The sample file has it corrected.
        string withTypo = Samples.Text(Samples.Template).Replace("Width=\"$WIDTH$\"", "Width=\"$WIDTH$\"\"");

        LabelSet set = LabelSetBuilder.Build(withTypo, csv: null, Now);

        Assert.NotNull(set.Error);
        Assert.Empty(set.Labels);
    }

    [Fact]
    public void AnotherXmlFile_IsNotALabelFile()
    {
        LabelSet set = LabelSetBuilder.Build("<DSSBatch><BatchName>X</BatchName></DSSBatch>", csv: null, Now);

        Assert.Equal(Strings.PreviewNotALabelFile, set.Error);
    }

    [Fact]
    public void TheEmptyLabelFileOfTheManual_IsValid_WithoutLabels()
    {
        // "To not print labels, an "empty" label-xml is required" (p. 29)
        const string empty = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<DSSPrintJob Length=\"2800\" Width=\"2100\" Turned=\"false\">\n</DSSPrintJob>";

        LabelSet set = LabelSetBuilder.Build(empty, csv: null, Now);

        Assert.Null(set.Error);
        Assert.Equal(Strings.PreviewEmptyLabelFile, set.Message);
        Assert.Empty(set.Labels);
    }

    [Fact]
    public void ACsvWithOnlyAHeader_GivesAMessage()
    {
        LabelCsv csv = LabelCsv.Parse("LENGTH\tWIDTH\tID\n", "empty.csv");

        LabelSet set = LabelSetBuilder.Build(Samples.Text(Samples.Template), csv, Now);

        Assert.Equal(Strings.PreviewCsvWithoutRows, set.Message);
    }

    [Theory]
    [InlineData(Samples.Template, true)]
    [InlineData(Samples.LabelXml, false)]  // $DATE$ in a Text does not make it a template
    public void ATemplate_IsRecognisedByTheVariablesInThePlaceOfItsLabel(string sample, bool expected)
    {
        Assert.Equal(expected, LabelSetBuilder.IsTemplate(Samples.Text(sample)));
    }

    [Fact]
    public void NamesOfNodesAndAttributes_AreReadInAnyCapitals()
    {
        var label = new XElement("PRINTLABEL", new XAttribute("rotation", "180"), new XAttribute("x", "12.5"));

        var item = new LabelItem(label, new XElement("DSSPrintJob"), new LabelVariables(null, Now));

        Assert.Equal(180, item.Rotation);
        Assert.Equal(12.5, item.X);
        Assert.True(LabelXml.Is(label, "PrintLabel"));
    }
}
