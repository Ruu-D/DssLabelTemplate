using System.Reflection;

namespace DssLabelTemplate;

/// <summary>
/// The folder of the program on the PC: C:\DssLabelTemplate (decided by Daan, like C:\TsDssConverter of the sister
/// program). It holds settings.json and two folders with example files that come with the exe:
///   labels\         the label-xml and template examples (samples\pdf-manual\*.xml in the repository);
///   CSV examples\   the label-csv examples (samples\pdf-manual\*.csv).
/// The examples are built INTO the exe (see the csproj), so the delivery stays one file.
/// </summary>
internal static class AppDataFolder
{
    public const string DefaultRoot = @"C:\DssLabelTemplate";

    public const string LabelsFolderName = "labels";
    public const string CsvFolderName = "CSV examples";

    // The names the csproj gives the packed examples: "examples/labels/<file>" and "examples/csv/<file>".
    private const string LabelsResourcePrefix = "examples/labels/";
    private const string CsvResourcePrefix = "examples/csv/";

    public static string SettingsFile(string root) => Path.Combine(root, "settings.json");
    public static string LabelsFolder(string root) => Path.Combine(root, LabelsFolderName);
    public static string CsvFolder(string root) => Path.Combine(root, CsvFolderName);

    /// <summary>
    /// Makes the folder and, when the labels or csv folder is not there yet (the first start), that folder with its
    /// examples. An existing folder is never touched: an example the user deleted or changed stays so.
    /// May throw IOException / UnauthorizedAccessException.
    /// </summary>
    public static void EnsureCreated(string root)
    {
        Directory.CreateDirectory(root);
        WriteExamples(LabelsFolder(root), LabelsResourcePrefix);
        WriteExamples(CsvFolder(root), CsvResourcePrefix);
    }

    /// <summary>The file names of the examples that go into a folder (for the tests).</summary>
    internal static List<string> PackedLabelExamples() => ResourceNames(LabelsResourcePrefix).Select(name => name[LabelsResourcePrefix.Length..]).ToList();
    internal static List<string> PackedCsvExamples() => ResourceNames(CsvResourcePrefix).Select(name => name[CsvResourcePrefix.Length..]).ToList();

    private static void WriteExamples(string folder, string resourcePrefix)
    {
        if (Directory.Exists(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        foreach (string resourceName in ResourceNames(resourcePrefix))
        {
            using Stream packed = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
            using FileStream file = File.Create(Path.Combine(folder, resourceName[resourcePrefix.Length..]));
            packed.CopyTo(file); // byte for byte: the encoding and line endings of the example stay as they are
        }
    }

    private static IEnumerable<string> ResourceNames(string prefix)
    {
        return Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal);
    }
}
