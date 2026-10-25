// The language of the interface is one switch for the whole program (Localizer.Current).
// Tests that change it must not run at the same time as tests that read texts in the default language,
// so the tests run one after the other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DssLabelTemplate.Tests;

/// <summary>Sets the language for one test and puts Dutch back afterwards (Dutch is the default).</summary>
public sealed class LanguageScope : IDisposable
{
    public LanguageScope(AppLanguage language)
    {
        Localizer.Current = language;
    }

    public void Dispose()
    {
        Localizer.Current = AppLanguage.Dutch;
    }
}

/// <summary>A new empty folder in %TEMP% for one test, deleted afterwards.</summary>
public sealed class TempFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DssLabelTemplateTests-" + Guid.NewGuid().ToString("N"));

    public TempFolder()
    {
        Directory.CreateDirectory(Path);
    }

    public string File(params string[] names) => System.IO.Path.Combine([Path, .. names]);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // a virus scanner may hold a file for a moment: %TEMP% is cleaned by Windows anyway
        }
    }
}

/// <summary>The example files of Duivestein's manual in samples\pdf-manual (see the README.txt there).</summary>
internal static class Samples
{
    public const string Template = "label-template.xml";
    public const string Csv = "label-example.csv";
    public const string LabelXml = "label-example.xml";

    /// <summary>The repository folder: the folder above the test output that holds DssLabelTemplate.slnx.</summary>
    public static string RepositoryFolder
    {
        get
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);
            while (folder != null && !System.IO.File.Exists(Path.Combine(folder.FullName, "DssLabelTemplate.slnx")))
            {
                folder = folder.Parent;
            }

            return folder?.FullName ?? throw new InvalidOperationException("DssLabelTemplate.slnx not found above " + AppContext.BaseDirectory);
        }
    }

    public static string File(string name) => Path.Combine(RepositoryFolder, "samples", "pdf-manual", name);

    public static string Text(string name) => TextFile.Read(File(name)).Text;
}

internal static class Sta
{
    /// <summary>Windows Forms needs its own STA thread; a failure there is passed back to the test.</summary>
    public static void Run(Action action)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure != null)
        {
            throw new Exception("The test failed on the STA thread: " + failure.Message, failure);
        }
    }
}
