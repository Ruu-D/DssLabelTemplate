namespace DssLabelTemplate;

/// <summary>
/// Code 128 (code set B: letters, digits and punctuation), to draw a barcode in the preview that looks like the real one.
/// The preview draws every barcode type of the manual (BARCODE128A/B/C, 39, 93, EAN) as Code 128 B: it shows where
/// the barcode sits and how big it is, it is not meant to be scanned. The real barcode is made by DSSPrintServer.
/// </summary>
internal static class Code128
{
    // The widths of bar, space, bar, space, bar, space for each of the 107 symbols (0 – 106), in modules.
    // Every symbol is 11 modules wide; the stop symbol (106) has a 7th element and is 13 wide (a test checks this).
    private static readonly string[] Patterns =
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112",
    };

    public const int StartB = 104;
    public const int Stop = 106;

    internal static IReadOnlyList<string> AllPatterns => Patterns;

    /// <summary>The symbol values: start B, one per character, the check symbol, stop.</summary>
    public static List<int> Values(string text)
    {
        var values = new List<int> { StartB };
        foreach (char c in text)
        {
            // Code set B has the characters 32 – 127; anything else is drawn as '?'.
            values.Add(c >= 32 && c <= 127 ? c - 32 : '?' - 32);
        }

        values.Add(Checksum(values));
        values.Add(Stop);
        return values;
    }

    /// <summary>The check symbol: (start + every value × its position) modulo 103.</summary>
    public static int Checksum(IReadOnlyList<int> startAndData)
    {
        int sum = startAndData[0];
        for (int i = 1; i < startAndData.Count; i++)
        {
            sum += startAndData[i] * i;
        }

        return sum % 103;
    }

    /// <summary>The barcode as modules from left to right: true = black bar, false = white space.</summary>
    public static bool[] Modules(string text)
    {
        var modules = new List<bool>();
        foreach (int value in Values(text))
        {
            bool bar = true;
            foreach (char width in Patterns[value])
            {
                modules.AddRange(Enumerable.Repeat(bar, width - '0'));
                bar = !bar;
            }
        }

        return modules.ToArray();
    }
}
