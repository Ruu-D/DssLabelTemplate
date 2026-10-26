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
    /// How far the sheet map below the label is turned, in degrees counter-clockwise (Daan, 2026-10-26): 0, 90, 180, 270
    /// or -90, -180, -270 (= clockwise). The label PC sits on a console that can be turned, so the operator may look at the
    /// sheet on the machine from another side than along its length. ONLY in settings.json, not in the window. Write the
    /// number without a plus sign ("90", not "+90": that is not valid json and the whole file would give the defaults).
    /// </summary>
    public int SheetRotationInViewer { get; set; }

    public static bool IsValidSheetRotation(int degrees) => degrees % 90 == 0 && degrees >= -270 && degrees <= 270;

    /// <summary>
    /// True: the preview shows the label turned by its ROTATION (from the csv), as it will sit on the sheet; false: upright,
    /// as printed (Daan, 2026-10-26). Only the preview; ONLY in settings.json, not in the window.
    /// </summary>
    public bool ShowLabelRotation { get; set; }

    /// <summary>
    /// True: the print is turned by the label's ROTATION (from the csv), around the middle of the label (Daan, 2026-10-26).
    /// This only works for a SQUARE label (length = width): a turned label of another shape does not fit its paper, so
    /// the window then asks whether to print it upright. ONLY in settings.json, not in the window.
    /// </summary>
    public bool PrintLabelWithRotation { get; set; }

    /// <summary>
    /// Which way a positive ROTATION of a label (from the csv) turns it on the sheet: "CCW" (counter-clockwise, default) or
    /// "CW" (clockwise) (Daan, 2026-10-26). Used by ShowLabelRotation and PrintLabelWithRotation; to be checked on the
    /// machine with a label at 90 or 270. ONLY in settings.json, not in the window. Anything else is "CCW" on loading.
    /// </summary>
    public string LabelDirection { get; set; } = CounterClockwise;

    public const string CounterClockwise = "CCW";
    public const string Clockwise = "CW";

    /// <summary>True when <see cref="LabelDirection"/> is "CW". Not written to settings.json (LabelDirection is).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool LabelRotationIsClockwise => string.Equals(LabelDirection, Clockwise, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The corner of the sheet the label X / Y in the csv are measured from (Daan, 2026-10-26): DSSClient's own names
    /// "Linksonder", "Linksboven", "Rechtsonder" or "Rechtsboven" (the English names work too, see <see cref="SheetCorners"/>).
    /// Must be the same corner as DSSClient's "Oriëntatie" and TsDssConverter's zero-point drop-down: TsDssConverter
    /// converts TopSolid's bottom-left positions to that corner, so with another corner here the sheet map would show the
    /// labels mirrored. ONLY in settings.json, not in the window. Anything else is "Linksonder" on loading.
    /// </summary>
    public string SheetReferenceCorner { get; set; } = SheetCorners.Name(SheetCorner.BottomLeft);

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

/// <summary>The four corners of a sheet that the label X / Y can be measured from (<see cref="AppSettings.SheetReferenceCorner"/>).</summary>
public enum SheetCorner
{
    BottomLeft,
    TopLeft,
    BottomRight,
    TopRight,
}

/// <summary>The names of the <see cref="SheetCorner"/>s in settings.json.</summary>
internal static class SheetCorners
{
    // DSSClient's own (Dutch) name first: that is the one written to settings.json. The others are also understood.
    private static readonly (SheetCorner Corner, string[] Names)[] KnownNames =
    {
        (SheetCorner.BottomLeft, new[] { "Linksonder", "BottomLeft", "Bottom-left" }),
        (SheetCorner.TopLeft, new[] { "Linksboven", "TopLeft", "Top-left" }),
        (SheetCorner.BottomRight, new[] { "Rechtsonder", "BottomRight", "Bottom-right" }),
        (SheetCorner.TopRight, new[] { "Rechtsboven", "TopRight", "Top-right" }),
    };

    /// <summary>The corner with this name (any case, spaces around it allowed), or null when the name is unknown.</summary>
    public static SheetCorner? Parse(string? name)
    {
        foreach (var (corner, names) in KnownNames)
        {
            if (names.Any(known => string.Equals(known, name?.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return corner;
            }
        }

        return null;
    }

    /// <summary>DSSClient's name of the corner, as written to settings.json.</summary>
    public static string Name(SheetCorner corner) => KnownNames.First(known => known.Corner == corner).Names[0];
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

            // A sheet rotation edited by hand into something that is not a quarter turn (e.g. 45): the sheet straight again.
            if (!AppSettings.IsValidSheetRotation(settings.SheetRotationInViewer))
            {
                settings.SheetRotationInViewer = 0;
            }

            // The label direction edited by hand: "cw" is written back as "CW"; anything else is counter-clockwise.
            settings.LabelDirection = settings.LabelRotationIsClockwise ? AppSettings.Clockwise : AppSettings.CounterClockwise;

            // A corner name edited by hand: written back with DSSClient's name; one that is unknown: the bottom left corner.
            settings.SheetReferenceCorner = SheetCorners.Name(SheetCorners.Parse(settings.SheetReferenceCorner) ?? SheetCorner.BottomLeft);

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
