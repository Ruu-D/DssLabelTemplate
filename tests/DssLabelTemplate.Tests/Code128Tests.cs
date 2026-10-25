namespace DssLabelTemplate.Tests;

public class Code128Tests
{
    [Fact]
    public void ThereAre107Symbols_Each11ModulesWide_AndTheStopIs13()
    {
        IReadOnlyList<string> patterns = Code128.AllPatterns;

        Assert.Equal(107, patterns.Count);
        for (int i = 0; i < patterns.Count; i++)
        {
            int width = patterns[i].Sum(c => c - '0');
            Assert.True(width == (i == Code128.Stop ? 13 : 11), $"symbol {i} is {width} modules wide");
        }

        Assert.Equal(patterns.Count, patterns.Distinct().Count()); // every symbol is different
    }

    [Fact]
    public void TheCheckSymbol_IsTheWeightedSumModulo103()
    {
        // "PJJ123C": 104 + 48·1 + 42·2 + 42·3 + 17·4 + 18·5 + 19·6 + 35·7 = 879; 879 mod 103 = 55
        List<int> values = Code128.Values("PJJ123C");

        Assert.Equal(Code128.StartB, values[0]);
        Assert.Equal(55, values[^2]);
        Assert.Equal(Code128.Stop, values[^1]);
    }

    [Fact]
    public void TheModules_StartWithStartB_AndHaveTheRightLength()
    {
        bool[] modules = Code128.Modules("PJJ123C");

        // start + 7 characters + check symbol = 9 symbols of 11, and the stop of 13
        Assert.Equal(9 * 11 + 13, modules.Length);
        string start = string.Concat(modules.Take(11).Select(bar => bar ? '1' : '0'));
        Assert.Equal("11010010000", start); // 211214: bar 2, space 1, bar 1, space 2, bar 1, space 4
        Assert.True(modules[^1]); // ends with a bar
    }
}
