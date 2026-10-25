namespace DssLabelTemplate;

/// <summary>
/// A label-csv of Duivestein: a header line with column names, then one line per label (DSS Software Interface,
/// "Label-csv example"). The separator may be a tab, a semicolon or a comma; it is taken from the header line.
/// Column names are matched without regard to capitals, like the $NAME$ variables of a template.
/// </summary>
internal class LabelCsv
{
    public string FilePath { get; }
    public IReadOnlyList<string> Columns { get; }

    /// <summary>One dictionary per label: column name (any case) → value.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows { get; }

    private LabelCsv(string filePath, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        FilePath = filePath;
        Columns = columns;
        Rows = rows;
    }

    /// <summary>Reads the file (any encoding, see TextFile). May throw IOException / UnauthorizedAccessException.</summary>
    public static LabelCsv Read(string path)
    {
        return Parse(TextFile.Read(path).Text, Path.GetFullPath(path));
    }

    public static LabelCsv Parse(string text, string filePath)
    {
        List<string> lines = text.Replace("\r\n", "\n").Split('\n').Where(line => line.Trim() != "").ToList();
        if (lines.Count == 0)
        {
            return new LabelCsv(filePath, Array.Empty<string>(), Array.Empty<IReadOnlyDictionary<string, string>>());
        }

        char separator = DetectSeparator(lines[0]);
        List<string> columns = SplitLine(lines[0], separator);
        var rows = new List<IReadOnlyDictionary<string, string>>();

        foreach (string line in lines.Skip(1))
        {
            List<string> fields = SplitLine(line, separator);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < columns.Count; i++)
            {
                // A short line: the missing fields are empty. A column name used twice: the first one counts.
                row.TryAdd(columns[i], i < fields.Count ? fields[i] : "");
            }

            rows.Add(row);
        }

        return new LabelCsv(filePath, columns, rows);
    }

    /// <summary>Tab if the header has one; otherwise the semicolon or the comma, whichever the header has more of.</summary>
    internal static char DetectSeparator(string header)
    {
        if (header.Contains('\t'))
        {
            return '\t';
        }

        int semicolons = header.Count(c => c == ';');
        int commas = header.Count(c => c == ',');
        return semicolons > 0 && semicolons >= commas ? ';' : ',';
    }

    /// <summary>Splits one line. A field between double quotes may contain the separator; "" inside quotes is one quote.</summary>
    internal static List<string> SplitLine(string line, char separator)
    {
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        bool quoted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == separator)
            {
                fields.Add(field.ToString().Trim());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString().Trim());
        return fields;
    }
}
