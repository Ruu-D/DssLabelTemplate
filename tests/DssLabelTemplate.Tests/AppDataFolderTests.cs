namespace DssLabelTemplate.Tests;

/// <summary>C:\DssLabelTemplate with settings.json and the example folders. The tests only use temporary folders.</summary>
public class AppDataFolderTests
{
    [Fact]
    public void EveryExampleOfTheRepository_IsPackedIntoTheExe()
    {
        string samples = Path.GetDirectoryName(Samples.File(Samples.Template))!;
        IEnumerable<string> Names(string pattern) => Directory.GetFiles(samples, pattern).Select(path => Path.GetFileName(path)!).Order(StringComparer.Ordinal);

        Assert.Equal(Names("*.xml"), AppDataFolder.PackedLabelExamples());
        Assert.Equal(Names("*.csv"), AppDataFolder.PackedCsvExamples());
        Assert.Contains(Samples.Template, AppDataFolder.PackedLabelExamples());
        Assert.Contains(Samples.Csv, AppDataFolder.PackedCsvExamples());
    }

    [Fact]
    public void TheFirstStart_WritesTheExamples_ByteForByte()
    {
        using var folder = new TempFolder();
        string root = folder.File("DssLabelTemplate"); // does not exist yet

        AppDataFolder.EnsureCreated(root);

        foreach (string name in AppDataFolder.PackedLabelExamples())
        {
            Assert.Equal(File.ReadAllBytes(Samples.File(name)), File.ReadAllBytes(Path.Combine(root, "labels", name)));
        }

        foreach (string name in AppDataFolder.PackedCsvExamples())
        {
            Assert.Equal(File.ReadAllBytes(Samples.File(name)), File.ReadAllBytes(Path.Combine(root, "CSV examples", name)));
        }
    }

    [Fact]
    public void AnExistingExampleFolder_IsNeverTouched()
    {
        using var folder = new TempFolder();
        string labels = Directory.CreateDirectory(folder.File("labels")).FullName;
        File.WriteAllText(Path.Combine(labels, Samples.Template), "changed by the user");

        AppDataFolder.EnsureCreated(folder.Path);

        // The user's own version stays, and examples the user deleted do not come back.
        Assert.Equal("changed by the user", File.ReadAllText(Path.Combine(labels, Samples.Template)));
        Assert.Single(Directory.GetFiles(labels));
        Assert.NotEmpty(Directory.GetFiles(folder.File("CSV examples"))); // that folder was missing: it was made
    }

    [Fact]
    public void TheSettings_LiveInTheFolderItself()
    {
        Assert.Equal(@"C:\DssLabelTemplate", AppDataFolder.DefaultRoot);
        Assert.Equal(@"D:\Somewhere\settings.json", AppDataFolder.SettingsFile(@"D:\Somewhere"));
    }
}
