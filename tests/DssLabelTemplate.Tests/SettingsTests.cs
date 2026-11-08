namespace DssLabelTemplate.Tests;

public class SettingsTests
{
    [Fact]
    public void NoFile_GivesTheDefaults()
    {
        using var folder = new TempFolder();

        AppSettings settings = SettingsStore.Load(folder.File("settings.json"));

        Assert.Equal("nl", settings.Language);
        Assert.Equal("", settings.LastFile);
    }

    // ---------------------------------------------------------------- settings.json edited by hand while the program runs

    // The settings that are ONLY in settings.json (edited by hand); every other setting belongs to the window.
    private static readonly string[] HandEditedSettings =
    {
        "SheetRotationInViewer", "ShowLabelRotation", "PrintLabelWithRotation", "HideWindowsPrintDialog", "LabelDirection",
        "SheetReferenceCorner", "ShowBoardByDefault", "RowHeightCSV",
    };

    [Fact]
    public void RowHeightCSV_Is100ByDefault_AndIsSavedAndLoaded()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        Assert.Equal(100, new AppSettings().RowHeightCSV);

        SettingsStore.Save(path, new AppSettings { RowHeightCSV = 150 });

        Assert.Equal(150, SettingsStore.Load(path).RowHeightCSV);
        Assert.Contains("\"RowHeightCSV\": 150", File.ReadAllText(path));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(49)]
    [InlineData(301)]
    [InlineData(1000)]
    public void AnImpossibleRowHeightCSV_IsBackTo100(int percent)
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ \"RowHeightCSV\": " + percent + " }");

        Assert.Equal(100, SettingsStore.Load(path).RowHeightCSV);
    }

    [Fact]
    public void ShowBoardByDefault_IsOffByDefault_AndIsSavedAndLoaded()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        Assert.False(new AppSettings().ShowBoardByDefault);

        SettingsStore.Save(path, new AppSettings { ShowBoardByDefault = true });

        Assert.True(SettingsStore.Load(path).ShowBoardByDefault);
        Assert.Contains("\"ShowBoardByDefault\": true", File.ReadAllText(path));
    }

    private static readonly string[] WindowSettings =
    {
        "Language", "LastFile", "LastCsvFile", "LabelLength", "LabelWidth", "PrinterName", "EditorColumnShare", "CsvColumnOrder",
        "WindowLeft", "WindowTop", "WindowWidth", "WindowHeight", "WindowMaximized",
    };

    [Fact]
    public void EverySetting_IsEitherOfTheWindowOrEditedByHand()
    {
        // A new setting must be put in one of the two lists above, and in AppSettings.CopyHandEditedSettingsFrom /
        // HasSameHandEditedSettings if it is only in the file: otherwise a save would quietly throw away a hand edit.
        var names = typeof(AppSettings).GetProperties().Where(property => property.CanWrite).Select(property => property.Name).OrderBy(name => name);

        Assert.Equal(HandEditedSettings.Concat(WindowSettings).OrderBy(name => name), names);
    }

    [Fact]
    public void SaveKeepingHandEdits_TakesTheWindowsSettings_AndKeepsWhatWasEditedByHandInTheFile()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        // The program runs with the defaults; meanwhile someone edits the hidden settings (and the language) in Notepad.
        var inUse = new AppSettings();
        SettingsStore.Save(path, inUse);
        File.WriteAllText(path, """
            {
              "Language": "en",
              "SheetRotationInViewer": 90,
              "ShowLabelRotation": true,
              "PrintLabelWithRotation": true,
              "HideWindowsPrintDialog": true,
              "LabelDirection": "CW",
              "SheetReferenceCorner": "Rechtsboven"
            }
            """);

        // The window saves its own changes (it still has the old hidden values in memory).
        inUse.Language = "fr";
        inUse.LabelLength = 60;
        inUse.CsvColumnOrder = new List<string> { "PW", "PL" };
        inUse.WindowWidth = 1200;
        inUse.WindowHeight = 800; // a width without a height is forgotten on loading

        Assert.True(SettingsStore.SaveKeepingHandEdits(path, inUse));
        AppSettings onDisk = SettingsStore.Load(path);

        // The window wins for its own settings ...
        Assert.Equal("fr", onDisk.Language);
        Assert.Equal(60, onDisk.LabelLength);
        Assert.Equal(new[] { "PW", "PL" }, onDisk.CsvColumnOrder);
        Assert.Equal(1200, onDisk.WindowWidth);

        // ... the file for the hidden ones.
        Assert.Equal(90, onDisk.SheetRotationInViewer);
        Assert.True(onDisk.ShowLabelRotation);
        Assert.True(onDisk.PrintLabelWithRotation);
        Assert.True(onDisk.HideWindowsPrintDialog);
        Assert.Equal("CW", onDisk.LabelDirection);
        Assert.Equal("Rechtsboven", onDisk.SheetReferenceCorner);

        Assert.False(inUse.HideWindowsPrintDialog); // the settings in use are not changed by saving
    }

    [Fact]
    public void SaveKeepingHandEdits_AFileWithAMistake_IsNotOverwritten()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        const string broken = "{ \"SheetRotationInViewer\": +90 }"; // "+90" is not valid json
        File.WriteAllText(path, broken);

        Assert.False(SettingsStore.SaveKeepingHandEdits(path, new AppSettings { Language = "fr" }));
        Assert.Equal(broken, File.ReadAllText(path));
    }

    [Fact]
    public void SaveKeepingHandEdits_WithoutAFile_WritesTheSettingsInUse()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        Assert.True(SettingsStore.SaveKeepingHandEdits(path, new AppSettings { HideWindowsPrintDialog = true, Language = "fr" }));

        AppSettings onDisk = SettingsStore.Load(path);
        Assert.True(onDisk.HideWindowsPrintDialog);
        Assert.Equal("fr", onDisk.Language);
    }

    [Fact]
    public void Read_SaysWhetherTheFileCouldBeUsed()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        Assert.False(SettingsStore.Read(path).FileExisted);

        File.WriteAllText(path, "{ this is not json");
        SettingsReadResult broken = SettingsStore.Read(path);
        Assert.True(broken.FileExisted);
        Assert.NotEqual("", broken.Error);
        Assert.False(broken.ErrorIsTemporary);

        File.WriteAllText(path, "{ \"Language\": \"fr\" }");
        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) // e.g. Notepad is still writing it
        {
            Assert.True(SettingsStore.Read(path).ErrorIsTemporary);
        }

        SettingsReadResult good = SettingsStore.Read(path);
        Assert.Equal("", good.Error);
        Assert.Equal("fr", good.Settings.Language);
    }

    [Fact]
    public void ABrokenFile_GivesTheDefaults_AndDoesNotThrow()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ this is not json");

        Assert.Equal("nl", SettingsStore.Load(path).Language);
    }

    [Fact]
    public void SaveAndLoad_GiveTheSameSettings()
    {
        using var folder = new TempFolder();
        string path = folder.File("sub", "settings.json"); // the folder is made when it is missing

        SettingsStore.Save(path, new AppSettings
        {
            Language = "fr", LastFile = @"C:\Templates\label.xml", LastCsvFile = @"C:\Templates\labels.csv", LabelLength = 80, LabelWidth = 50.5,
        });
        AppSettings loaded = SettingsStore.Load(path);

        Assert.Equal("fr", loaded.Language);
        Assert.Equal(@"C:\Templates\label.xml", loaded.LastFile);
        Assert.Equal(@"C:\Templates\labels.csv", loaded.LastCsvFile);
        Assert.Equal(80, loaded.LabelLength);
        Assert.Equal(50.5, loaded.LabelWidth);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void AnImpossibleLabelSizeInTheFile_IsBackTo100()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ \"LabelLength\": 0, \"LabelWidth\": 99999 }");

        AppSettings settings = SettingsStore.Load(path);

        Assert.Equal(100, settings.LabelLength);
        Assert.Equal(100, settings.LabelWidth);
    }

    [Fact]
    public void AnImpossibleColumnShareInTheFile_IsBackToTwoEqualHalves_AndNullPrinterIsEmpty()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ \"EditorColumnShare\": 1.5, \"PrinterName\": null }");

        AppSettings settings = SettingsStore.Load(path);

        Assert.Equal(AppSettings.DefaultEditorColumnShare, settings.EditorColumnShare);
        Assert.Equal("", settings.PrinterName);
    }

    [Fact]
    public void ThePrinterAndTheColumnShare_AreSavedAndLoaded()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        SettingsStore.Save(path, new AppSettings { PrinterName = "ZDesigner ZD220-203dpi ZPL", EditorColumnShare = 0.35 });
        AppSettings loaded = SettingsStore.Load(path);

        Assert.Equal("ZDesigner ZD220-203dpi ZPL", loaded.PrinterName);
        Assert.Equal(0.35, loaded.EditorColumnShare);
    }

    [Fact]
    public void TheSheetRotation_IsSavedAndLoaded_AlsoNegative()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        SettingsStore.Save(path, new AppSettings { SheetRotationInViewer = -90 });

        Assert.Contains("\"SheetRotationInViewer\": -90", File.ReadAllText(path)); // the name a person edits by hand
        Assert.Equal(-90, SettingsStore.Load(path).SheetRotationInViewer);
    }

    [Fact]
    public void ASheetRotationThatIsNoQuarterTurn_IsBackTo0()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        foreach (int degrees in new[] { 45, 360, -450 })
        {
            File.WriteAllText(path, "{ \"SheetRotationInViewer\": " + degrees + " }");
            Assert.Equal(0, SettingsStore.Load(path).SheetRotationInViewer);
        }

        File.WriteAllText(path, "{ \"Language\": \"en\" }"); // a settings.json of version 1.1.1
        Assert.Equal(0, SettingsStore.Load(path).SheetRotationInViewer);
    }

    [Fact]
    public void TheTwoRotationSettings_AreOffByDefault_AndSavedAndLoaded()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        File.WriteAllText(path, "{ \"Language\": \"en\" }"); // a settings.json of before
        Assert.False(SettingsStore.Load(path).ShowLabelRotation);
        Assert.False(SettingsStore.Load(path).PrintLabelWithRotation);
        Assert.False(SettingsStore.Load(path).HideWindowsPrintDialog);

        SettingsStore.Save(path, new AppSettings { ShowLabelRotation = true, PrintLabelWithRotation = true, HideWindowsPrintDialog = true });

        string text = File.ReadAllText(path);
        Assert.Contains("\"ShowLabelRotation\": true", text); // the names a person edits by hand
        Assert.Contains("\"PrintLabelWithRotation\": true", text);
        Assert.Contains("\"HideWindowsPrintDialog\": true", text);
        Assert.True(SettingsStore.Load(path).ShowLabelRotation);
        Assert.True(SettingsStore.Load(path).PrintLabelWithRotation);
        Assert.True(SettingsStore.Load(path).HideWindowsPrintDialog);
    }

    [Theory]
    [InlineData(null, "CCW", false)]   // a settings.json of before: counter-clockwise
    [InlineData("CW", "CW", true)]
    [InlineData("cw", "CW", true)]
    [InlineData("CCW", "CCW", false)]
    [InlineData("left", "CCW", false)] // unknown: counter-clockwise
    public void TheLabelDirection_IsCCWByDefault_AndCWWhenWritten(string? written, string loaded, bool clockwise)
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        File.WriteAllText(path, written == null ? "{ \"Language\": \"en\" }" : "{ \"LabelDirection\": \"" + written + "\" }");

        AppSettings settings = SettingsStore.Load(path);
        Assert.Equal(loaded, settings.LabelDirection);
        Assert.Equal(clockwise, settings.LabelRotationIsClockwise);
    }

    [Fact]
    public void TheLabelDirection_IsSavedByItsName_AndTheHelperIsNotWritten()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        SettingsStore.Save(path, new AppSettings { LabelDirection = "CW" });

        string text = File.ReadAllText(path);
        Assert.Contains("\"LabelDirection\": \"CW\"", text);
        Assert.DoesNotContain("LabelRotationIsClockwise", text);
    }

    [Fact]
    public void TheCsvColumnOrder_IsSavedAndLoaded_EmptyByDefault_WithoutBlankNames()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        File.WriteAllText(path, "{ \"Language\": \"en\" }"); // a settings.json of before
        Assert.Empty(SettingsStore.Load(path).CsvColumnOrder);

        SettingsStore.Save(path, new AppSettings { CsvColumnOrder = new List<string> { "ID", "DESCRIPTION" } });
        Assert.Contains("\"CsvColumnOrder\": [", File.ReadAllText(path));
        Assert.Equal(new[] { "ID", "DESCRIPTION" }, SettingsStore.Load(path).CsvColumnOrder);

        File.WriteAllText(path, "{ \"CsvColumnOrder\": null }");
        Assert.Empty(SettingsStore.Load(path).CsvColumnOrder);

        File.WriteAllText(path, "{ \"CsvColumnOrder\": [ \"ID\", \"\", \"  \", \"X\" ] }");
        Assert.Equal(new[] { "ID", "X" }, SettingsStore.Load(path).CsvColumnOrder);
    }

    [Fact]
    public void TheReferenceCorner_IsSavedAndLoaded_WithDSSClientsName()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        SettingsStore.Save(path, new AppSettings { SheetReferenceCorner = "Rechtsboven" });

        Assert.Contains("\"SheetReferenceCorner\": \"Rechtsboven\"", File.ReadAllText(path)); // the name a person edits by hand
        Assert.Equal("Rechtsboven", SettingsStore.Load(path).SheetReferenceCorner);
    }

    [Theory]
    [InlineData("TopRight", "Rechtsboven")]
    [InlineData(" top-left ", "Linksboven")]
    [InlineData("rechtsonder", "Rechtsonder")]
    [InlineData("Bottom-left", "Linksonder")]
    [InlineData("Midden", "Linksonder")]   // unknown: the bottom left corner
    [InlineData("", "Linksonder")]
    public void AReferenceCornerEditedByHand_IsUnderstood_OrBackToLinksonder(string written, string loaded)
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        File.WriteAllText(path, "{ \"SheetReferenceCorner\": \"" + written + "\" }");

        Assert.Equal(loaded, SettingsStore.Load(path).SheetReferenceCorner);
    }

    [Fact]
    public void ASettingsFileWithoutAReferenceCorner_GivesLinksonder()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");

        File.WriteAllText(path, "{ \"Language\": \"en\" }"); // a settings.json of version 1.1.2
        Assert.Equal("Linksonder", SettingsStore.Load(path).SheetReferenceCorner);

        File.WriteAllText(path, "{ \"SheetReferenceCorner\": null }");
        Assert.Equal("Linksonder", SettingsStore.Load(path).SheetReferenceCorner);
    }

    [Fact]
    public void ASettingsFileWithoutALabelSize_Gives100By100()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ \"Language\": \"en\" }"); // a settings.json of version 0.2.0

        AppSettings settings = SettingsStore.Load(path);

        Assert.Equal(100, settings.LabelLength);
        Assert.Equal(100, settings.LabelWidth);
    }
}
