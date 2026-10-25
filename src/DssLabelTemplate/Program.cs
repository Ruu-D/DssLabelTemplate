namespace DssLabelTemplate;

/// <summary>The command line. The user just starts the exe (or drops a label file on it).</summary>
internal class StartupOptions
{
    /// <summary>--data &lt;folder&gt;: use another folder than C:\DssLabelTemplate (so a test never touches the real one).</summary>
    public string? DataFolder { get; private set; }

    /// <summary>A file to open at the start (Explorer's "Open with", or a file dropped on the exe).</summary>
    public string? FileToOpen { get; private set; }

    /// <summary>--write-icon &lt;folder&gt;: only for the developer: write app.ico and the PNGs of the logo, then stop.</summary>
    public string? IconFolder { get; private set; }

    public static StartupOptions Parse(string[] args)
    {
        var options = new StartupOptions();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--data" && i + 1 < args.Length)
            {
                i++;
                options.DataFolder = args[i];
            }
            else if (args[i] == "--write-icon" && i + 1 < args.Length)
            {
                i++;
                options.IconFolder = args[i];
            }
            else if (!args[i].StartsWith("--"))
            {
                options.FileToOpen = args[i];
            }
        }

        return options;
    }
}

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var options = StartupOptions.Parse(args);
        if (options.IconFolder != null)
        {
            IconFileWriter.WriteAll(options.IconFolder); // see AppLogo.cs
            return;
        }

        string dataFolder = options.DataFolder ?? AppDataFolder.DefaultRoot;
        string settingsFile = AppDataFolder.SettingsFile(dataFolder);

        AppSettings settings = SettingsStore.Load(settingsFile);
        Localizer.Current = Localizer.FromCode(settings.Language);

        ApplicationConfiguration.Initialize();

        // C:\DssLabelTemplate with settings.json and the example folders (only what is missing is written).
        try
        {
            AppDataFolder.EnsureCreated(dataFolder);
            if (!File.Exists(settingsFile))
            {
                SettingsStore.Save(settingsFile, settings);
            }
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            MessageBox.Show(Strings.DataFolderFailed(dataFolder, error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // The XML editor needs its two native files on disk before the window (and so the editor) is made.
        try
        {
            ScintillaNET.ScintillaNativeLibrary.SatelliteDirectory = ScintillaFiles.Extract(ScintillaFiles.DefaultFolder);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            MessageBox.Show(Strings.EditorFilesFailed(ScintillaFiles.DefaultFolder, error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var window = new MainForm(settings, changed => SettingsStore.Save(settingsFile, changed), options.FileToOpen, dataFolder);
        Application.Run(window);
    }
}
