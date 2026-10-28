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
