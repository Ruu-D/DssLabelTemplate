namespace DssLabelTemplate.Tests;

/// <summary>
/// The sheet map below the label, turned by SheetRotationInViewer (settings.json only). A sheet of 2850 x 2100 (X along
/// the length, Y upwards from the bottom left corner) in an area of 400 x 200 pixels.
/// </summary>
public class SheetMapTests
{
    private static readonly RectangleF Area = new(0, 0, 400, 200);
    private const float Length = 2850, Width = 2100;

    private static PointF BottomLeftCorner(int degrees) =>
        LabelPreview.MapPoint(Area, Length, Width, LabelPreview.QuarterTurns(degrees), 0, 0);

    private static SizeF SheetOnScreen(int degrees)
    {
        int turns = LabelPreview.QuarterTurns(degrees);
        PointF a = LabelPreview.MapPoint(Area, Length, Width, turns, 0, 0);
        PointF b = LabelPreview.MapPoint(Area, Length, Width, turns, Length, Width);
        return new SizeF(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 1)]
    [InlineData(180, 2)]
    [InlineData(270, 3)]
    [InlineData(-90, 3)]
    [InlineData(-180, 2)]
    [InlineData(-270, 1)]
    public void TheDegrees_GiveQuarterTurnsCounterClockwise(int degrees, int turns)
    {
        Assert.Equal(turns, LabelPreview.QuarterTurns(degrees));
    }

    [Fact]
    public void Straight_TheBottomLeftCornerIsBottomLeft_AndTheSheetLiesFlat()
    {
        PointF corner = BottomLeftCorner(0);
        Assert.True(corner.X < Area.Width / 2 && corner.Y > Area.Height / 2, corner.ToString());
        Assert.True(SheetOnScreen(0).Width > SheetOnScreen(0).Height);
    }

    [Fact]
    public void AQuarterTurn_StandsTheSheetUp_AndMovesTheBottomLeftCornerAround()
    {
        // +90 = counter-clockwise: the bottom left corner goes to the bottom right; -90 = clockwise: to the top left.
        PointF plus = BottomLeftCorner(90), minus = BottomLeftCorner(-90);
        Assert.True(plus.X > Area.Width / 2 && plus.Y > Area.Height / 2, "+90: " + plus);
        Assert.True(minus.X < Area.Width / 2 && minus.Y < Area.Height / 2, "-90: " + minus);

        SizeF sheet = SheetOnScreen(90);
        Assert.True(sheet.Height > sheet.Width, "upright: " + sheet);
        Assert.InRange(sheet.Height, Area.Height - 0.01f, Area.Height + 0.01f); // still as big as fits
        Assert.InRange(sheet.Width / sheet.Height, Width / Length - 0.001f, Width / Length + 0.001f); // not stretched
    }

    [Fact]
    public void HalfATurn_PutsTheBottomLeftCornerTopRight()
    {
        PointF corner = BottomLeftCorner(180);
        Assert.True(corner.X > Area.Width / 2 && corner.Y < Area.Height / 2, corner.ToString());
        Assert.Equal(SheetOnScreen(0), SheetOnScreen(180));
    }

    [Theory]
    [InlineData(SheetCorner.BottomLeft, 399, 540)]
    [InlineData(SheetCorner.BottomRight, 2451, 540)]
    [InlineData(SheetCorner.TopLeft, 399, 1560)]
    [InlineData(SheetCorner.TopRight, 2451, 1560)]
    public void ThePositionOfTheCsv_MeasuredFromItsCorner_LandsOnTheSameSpotOfTheSheet(SheetCorner corner, double x, double y)
    {
        // K1 - Front - 3575 of the sample: TopSolid says X 399, Y 540 from the bottom left; TsDssConverter writes the csv
        // measured from the corner of its drop-down. Every corner must give the same spot on the map.
        Assert.Equal((399.0, 540.0), LabelPreview.ToBottomLeft(corner, Length, Width, x, y));
    }

    [Theory]
    [InlineData(SheetCorner.BottomLeft, false, false)]
    [InlineData(SheetCorner.BottomRight, true, false)]
    [InlineData(SheetCorner.TopLeft, false, true)]
    [InlineData(SheetCorner.TopRight, true, true)]
    public void TheZeroPoint_IsOnTheChosenCornerOfTheSheet(SheetCorner corner, bool right, bool top)
    {
        var (x, y) = LabelPreview.ToBottomLeft(corner, Length, Width, 0, 0);
        PointF point = LabelPreview.MapPoint(Area, Length, Width, 0, x, y);
        Assert.Equal(right, point.X > Area.Width / 2);
        Assert.Equal(top, point.Y < Area.Height / 2); // on the screen Y counts downwards
    }

    [Theory]
    [InlineData(900, 8, 16)]
    [InlineData(1350, 12, 24)] // the same at 150%
    public void TheSheetMap_WithTheRoomAboveAndBelowIt_Gets40PercentOfThePreview(int height, int gap, int margin)
    {
        int map = LabelPreview.MapHeight(height, gap, margin);
        Assert.InRange((gap + map + margin) / (double)height, 0.399, 0.401);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(-90)]
    public void TheSheetStaysInTheMiddleOfTheArea(int degrees)
    {
        int turns = LabelPreview.QuarterTurns(degrees);
        PointF middle = LabelPreview.MapPoint(Area, Length, Width, turns, Length / 2, Width / 2);
        Assert.Equal(new PointF(200, 100), middle);
    }
}
