using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DssLabelTemplate;

// The label files of Duivestein (DSS Software Interface v1.7, "Label", "Label-csv example", "Label-xml example"):
//
//   <DSSPrintJob Length="2800" Width="2100">          the sheet (mm)
//     <PrintLabel ID="5436280" X="500" Y="1000">       one label: its place on the sheet (mm), optional ROTATION
//       <Text X="7" Y="47" FontSize="10">…</Text>      the print objects, in mm on the label
//     </PrintLabel>
//   </DSSPrintJob>
//
// A label-xml has all the labels of one sheet. A label TEMPLATE has one <PrintLabel> with variables ($XPOS$, $ORDERNO$ …)
// that are filled in from each line of a label-csv: one label per csv line.

/// <summary>What kind of file is in the editor.</summary>
internal enum LabelFileKind
{
    LabelXml,
    Template,
}

/// <summary>Small helpers for the label xml: names without regard to capitals, and numbers with a dot.</summary>
internal static class LabelXml
{
    public static bool Is(XElement element, string name)
    {
        return string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The raw value of an attribute (any capitals: the manual writes both "Rotation" and "ROTATION"), or null.</summary>
    public static string? Attribute(XElement element, string name)
    {
        return element.Attributes()
            .FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    /// <summary>A number in mm or points. The manual says "dot as decimal separator"; a comma is forgiven.</summary>
    public static bool TryNumber(string? text, out double value)
    {
        value = 0;
        return !string.IsNullOrWhiteSpace(text)
            && double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The line of the element in the editor (0 if unknown), so a problem can be found.</summary>
    public static int LineOf(XElement element)
    {
        var info = (IXmlLineInfo)element;
        return info.HasLineInfo() ? info.LineNumber : 0;
    }
}

/// <summary>The values for the $NAME$ variables of one label: its csv line, plus $DATE$ and $TIME$ (filled in when printing).</summary>
internal class LabelVariables
{
    /// <summary>A variable: $ + a csv column name (letters, digits, _ and -) + $. Also used to mark them in the editor.</summary>
    internal static readonly Regex Variable = new(@"\$([A-Za-z0-9_\-]+)\$");

    private readonly IReadOnlyDictionary<string, string>? _row;
    private readonly DateTime _now;

    public LabelVariables(IReadOnlyDictionary<string, string>? row, DateTime now)
    {
        _row = row;
        _now = now;
    }

    /// <summary>The text with every known variable filled in. An unknown variable stays as it is ($NAME$), so it is visible.</summary>
    public string Resolve(string text)
    {
        return Variable.Replace(text, match => Lookup(match.Groups[1].Value) ?? match.Value);
    }

    /// <summary>True when these values come from a csv line (a template with its label-csv).</summary>
    public bool HasCsvLine => _row != null;

    /// <summary>The names of the variables in the text that have no value: not a column of the csv (nor DATE / TIME).</summary>
    public IEnumerable<string> Unknown(string text)
    {
        return Variable.Matches(text).Select(match => match.Groups[1].Value).Where(name => Lookup(name) == null);
    }

    private string? Lookup(string name)
    {
        if (_row != null && _row.TryGetValue(name, out string? value))
        {
            return value;
        }

        // The format of the date and time on a real label is chosen by DSSPrintServer; this is only an example.
        if (name.Equals("DATE", StringComparison.OrdinalIgnoreCase))
        {
            return _now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        if (name.Equals("TIME", StringComparison.OrdinalIgnoreCase))
        {
            return _now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return null;
    }
}

/// <summary>One label to show: a &lt;PrintLabel&gt; node and the values for its variables.</summary>
internal class LabelItem
{
    public XElement Element { get; }
    public LabelVariables Variables { get; }

    public string Id { get; }
    public double? X { get; }
    public double? Y { get; }
    public int Rotation { get; }
    public double? SheetLength { get; }
    public double? SheetWidth { get; }

    /// <summary>The csv line this label was made from (0 = the first line after the header); null for a label-xml.</summary>
    public int? CsvRow { get; }

    /// <summary>
    /// The variables of the template that are not a column of the label-csv, e.g. "XPOS" when the template says
    /// X="$XPOS$" but the csv of TsDssConverter calls that column X. Empty without a csv (then every variable is empty).
    /// The preview names them, instead of only showing "X ?".
    /// </summary>
    public List<string> MissingVariables { get; } = new();

    public LabelItem(XElement printLabel, XElement printJob, LabelVariables variables, int? csvRow = null)
    {
        Element = printLabel;
        Variables = variables;
        CsvRow = csvRow;

        Id = Value(printLabel, "ID") ?? "";
        X = Number(Value(printLabel, "X"));
        Y = Number(Value(printLabel, "Y"));
        Rotation = Number(Value(printLabel, "Rotation")) is double rotation ? (int)Math.Round(rotation) : 0;
        SheetLength = Number(Value(printJob, "Length"));
        SheetWidth = Number(Value(printJob, "Width"));

        if (variables.HasCsvLine)
        {
            // Every place a variable can be: the sheet size, the place of the label, and every print object (its
            // attributes and its text), in the order of the file.
            IEnumerable<string> texts = printJob.Attributes().Select(attribute => attribute.Value)
                .Concat(printLabel.DescendantsAndSelf().SelectMany(element => element.Attributes()).Select(attribute => attribute.Value))
                .Concat(printLabel.DescendantNodes().OfType<XText>().Select(text => text.Value));
            MissingVariables.AddRange(texts.SelectMany(variables.Unknown).Distinct(StringComparer.OrdinalIgnoreCase));
        }
    }

    /// <summary>The value of an attribute with the variables filled in, or null if the attribute is not there.</summary>
    public string? Value(XElement element, string name)
    {
        string? raw = LabelXml.Attribute(element, name);
        return raw == null ? null : Variables.Resolve(raw);
    }

    /// <summary>The number of an attribute (variables filled in), or null if it is missing or not a number.</summary>
    public double? NumberOf(XElement element, string name) => Number(Value(element, name));

    private static double? Number(string? text) => LabelXml.TryNumber(text, out double value) ? value : null;
}

/// <summary>Everything the preview shows: the labels, or the reason there are none.</summary>
internal class LabelSet
{
    public LabelFileKind Kind { get; init; }
    public List<LabelItem> Labels { get; } = new();

    /// <summary>The xml cannot be used at all (not readable, not a DSSPrintJob). Shown in red.</summary>
    public string? Error { get; init; }

    /// <summary>A valid file without labels (an "empty" label file, a csv without lines). Shown in navy.</summary>
    public string? Message { get; init; }
}

/// <summary>Builds the <see cref="LabelSet"/> from the text in the editor and, for a template, the label-csv.</summary>
internal static class LabelSetBuilder
{
    public static LabelSet Build(string xmlText, LabelCsv? csv, DateTime now)
    {
        XDocument document;
        try
        {
            // SetLineInfo: a print object that cannot be drawn is reported with its line in the editor.
            document = XDocument.Parse(xmlText, LoadOptions.SetLineInfo);
        }
        catch (XmlException error)
        {
            return new LabelSet { Error = Strings.PreviewXmlError(error.Message) };
        }

        XElement printJob = document.Root!;
        if (!LabelXml.Is(printJob, "DSSPrintJob"))
        {
            return new LabelSet { Error = Strings.PreviewNotALabelFile };
        }

        List<XElement> printLabels = PrintLabels(printJob);
        LabelFileKind kind = IsTemplate(printJob) ? LabelFileKind.Template : LabelFileKind.LabelXml;

        if (printLabels.Count == 0)
        {
            return new LabelSet { Kind = kind, Message = Strings.PreviewEmptyLabelFile };
        }

        if (kind == LabelFileKind.Template && csv != null && csv.Rows.Count == 0)
        {
            return new LabelSet { Kind = kind, Message = Strings.PreviewCsvWithoutRows };
        }

        var set = new LabelSet { Kind = kind };
        if (kind == LabelFileKind.Template && csv != null)
        {
            // "This node is repeated for each data line in the csv-file during the merge."
            for (int row = 0; row < csv.Rows.Count; row++)
            {
                foreach (XElement printLabel in printLabels)
                {
                    set.Labels.Add(new LabelItem(printLabel, printJob, new LabelVariables(csv.Rows[row], now), row));
                }
            }
        }
        else
        {
            // A label-xml, or a template without data: the variables stay visible as $NAME$.
            foreach (XElement printLabel in printLabels)
            {
                set.Labels.Add(new LabelItem(printLabel, printJob, new LabelVariables(null, now)));
            }
        }

        return set;
    }

    /// <summary>
    /// A template has variables in the place of the label on the sheet (X="$XPOS$") or in the sheet size: in a label-xml
    /// these are real numbers. ($DATE$ and $TIME$ inside a Text do not count: a label-xml may use them too.)
    /// </summary>
    public static bool IsTemplate(XElement printJob)
    {
        return printJob.Attributes()
            .Concat(PrintLabels(printJob).SelectMany(printLabel => printLabel.Attributes()))
            .Any(attribute => attribute.Value.Contains('$'));
    }

    /// <summary>The same for a whole file text; false if it is not readable xml.</summary>
    public static bool IsTemplate(string xmlText)
    {
        try
        {
            XElement? root = XDocument.Parse(xmlText).Root;
            return root != null && IsTemplate(root);
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private static List<XElement> PrintLabels(XElement printJob)
    {
        return printJob.Elements().Where(element => LabelXml.Is(element, "PrintLabel")).ToList();
    }
}
