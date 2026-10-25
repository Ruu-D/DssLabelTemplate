using System.Text.Json;

namespace DssLabelTemplate;

/// <summary>What the program remembers between two starts (settings.json in C:\DssLabelTemplate, see <see cref="AppDataFolder"/>).</summary>
internal class AppSettings
{
    /// <summary>"nl", "fr" or "en" (see <see cref="Localizer"/>).</summary>
    public string Language { get; set; } = "nl";

    /// <summary>The label-xml or template that was open last; opened again at the next start. Empty = none.</summary>
    public string LastFile { get; set; } = "";

    /// <summary>The label-csv that belonged to the template of <see cref="LastFile"/>. Empty = none (a label-xml).</summary>
    public string LastCsvFile { get; set; } = "";

    /// <summary>The size of the label in mm: length = horizontal (along X), width = vertical (along Y). Not in the label files.</summary>
    public double LabelLength { get; set; } = DefaultLabelSize;
    public double LabelWidth { get; set; } = DefaultLabelSize;

    /// <summary>The label size when nothing else is known (mm): 100 x 100.</summary>
    public const double DefaultLabelSize = 100;

    /// <summary>The smallest and biggest label size the window accepts (mm).</summary>
    public const double MinimumLabelSize = 1;
    public const double MaximumLabelSize = 1000;

    public static bool IsValidLabelSize(double millimetres) => millimetres >= MinimumLabelSize && millimetres <= MaximumLabelSize;

    /// <summary>The printer of the last print (chosen in the print dialog); the dialog starts with it. Empty = Windows' default printer.</summary>
    public string PrinterName { get; set; } = "";

    /// <summary>
    /// The part of the width that the LEFT column (the editor) gets; the preview gets the rest. Set by dragging the vertical
    /// line between them. 0.5 = two equal halves.
    /// </summary>
    public double EditorColumnShare { get; set; } = DefaultEditorColumnShare;

    public const double DefaultEditorColumnShare = 0.5;
    public const double MinimumEditorColumnShare = 0.1;
    public const double MaximumEditorColumnShare = 0.9;

    public static bool IsValidEditorColumnShare(double share) => share >= MinimumEditorColumnShare && share <= MaximumEditorColumnShare;

    /// <summary>
    /// Where the window was when the program was closed last time (Daan, 2026-10-25), used again at the next start:
    /// the top left corner in screen pixels, the size at 100% (so it fits a monitor with another scaling too), and whether
    /// it was maximised. When maximised, the place and size are those of the normal window (to go back to).
    /// WindowWidth 0 = never saved: the standard size, in the middle of the screen.
    /// </summary>
    public int WindowLeft { get; set; }
    public int WindowTop { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }
}

/// <summary>Reads and writes settings.json. Never throws on reading: a missing or broken file gives the defaults.</summary>
internal static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip, // a person may edit the file by hand
        AllowTrailingCommas = true,
    };

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Options) ?? new AppSettings();

            // "null" in the file: back to the default
            settings.Language ??= "nl";
            settings.LastFile ??= "";
            settings.LastCsvFile ??= "";
            settings.PrinterName ??= "";

            // A window size edited by hand into something impossible: forget the place (standard size, in the middle).
            if (settings.WindowWidth <= 0 || settings.WindowHeight <= 0)
            {
                settings.WindowWidth = 0;
                settings.WindowHeight = 0;
            }

            // A column share edited by hand into something impossible: back to two equal halves.
            if (!AppSettings.IsValidEditorColumnShare(settings.EditorColumnShare))
            {
                settings.EditorColumnShare = AppSettings.DefaultEditorColumnShare;
            }

            // A size edited by hand into something impossible: back to 100 mm.
            if (!AppSettings.IsValidLabelSize(settings.LabelLength))
            {
                settings.LabelLength = AppSettings.DefaultLabelSize;
            }

            if (!AppSettings.IsValidLabelSize(settings.LabelWidth))
            {
                settings.LabelWidth = AppSettings.DefaultLabelSize;
            }

            return settings;
        }
        catch (Exception error) when (error is JsonException || error is IOException || error is UnauthorizedAccessException)
        {
            return new AppSettings(); // these settings are only conveniences: start with the defaults
        }
    }

    /// <summary>Saves as settings.json.tmp first, then renames: a crash never leaves half a file. May throw IOException.</summary>
    public static void Save(string path, AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Options));
        File.Move(temporary, path, overwrite: true);
    }
}
