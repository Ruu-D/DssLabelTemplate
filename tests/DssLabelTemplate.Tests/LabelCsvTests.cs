namespace DssLabelTemplate.Tests;

public class LabelCsvTests
{
    [Fact]
    public void TheExampleOfTheManual_HasTenColumnsAndThreeLabels()
    {
        LabelCsv csv = LabelCsv.Read(Samples.File(Samples.Csv));

        Assert.Equal(new[] { "LENGTH", "WIDTH", "ID", "XPOS", "YPOS", "ROTATION", "ORDERNO", "PW", "PH", "BARCODE1" }, csv.Columns);
        Assert.Equal(3, csv.Rows.Count);
        Assert.Equal("931", csv.Rows[0]["ID"]);
        Assert.Equal("350.2", csv.Rows[2]["PH"]);
        Assert.Equal("3629310-933", csv.Rows[1]["barcode1"]); // column names in any capitals, like the $NAME$ variables
    }

    [Theory]
    [InlineData("A\tB;C,D", '\t')]
    [InlineData("A;B;C", ';')]
    [InlineData("A,B,C", ',')]
    [InlineData("A;B,C", ';')]  // as many: the semicolon (Belgian Excel)
    [InlineData("ONECOLUMN", ',')]
    public void TheSeparator_IsTakenFromTheHeaderLine(string header, char expected)
    {
        Assert.Equal(expected, LabelCsv.DetectSeparator(header));
    }

    [Fact]
    public void AQuotedField_MayHoldTheSeparatorAndQuotes()
    {
        LabelCsv csv = LabelCsv.Parse("ID;TEXT\n1;\"a;b \"\"c\"\"\"\n", "test.csv");

        Assert.Equal("a;b \"c\"", csv.Rows[0]["TEXT"]);
    }

    [Fact]
    public void AShortLine_GetsEmptyFields_AndEmptyLinesAreSkipped()
    {
        LabelCsv csv = LabelCsv.Parse("A,B,C\r\n1,2\r\n\r\n", "test.csv");

        Assert.Single(csv.Rows);
        Assert.Equal("", csv.Rows[0]["C"]);
    }
}
