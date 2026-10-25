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
