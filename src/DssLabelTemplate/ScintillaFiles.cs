using System.Security.Cryptography;

namespace DssLabelTemplate;

/// <summary>
/// The two native Windows files of the XML editor (Scintilla.dll and Lexilla.dll, from the Scintilla5.NET package).
/// Windows can only load such a file from disk, not from inside our single-file exe. So they are built into the exe
/// (see the csproj) and written to a folder of their own once; the editor is then told to load them from there.
/// The folder name is made from the files' content, so a new version of the package gets a new folder and an old copy
/// is never mixed with a new one.
/// </summary>
internal static class ScintillaFiles
{
    private static readonly string[] FileNames = { "Scintilla.dll", "Lexilla.dll" };

    /// <summary>%LOCALAPPDATA%\DssLabelTemplate\scintilla: on this PC only (program files do not belong in a roaming profile).</summary>
    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Strings.AppName, "scintilla");

    /// <summary>
    /// Writes the two files into a sub folder of <paramref name="baseFolder"/> (only when they are not there yet) and
    /// returns that folder. May throw IOException / UnauthorizedAccessException.
    /// </summary>
    public static string Extract(string baseFolder)
    {
        var files = FileNames.Select(name => (Name: name, Bytes: ReadResource(name))).ToList();
        byte[] hash = SHA256.HashData(files.SelectMany(file => file.Bytes).ToArray());
        string folder = Path.Combine(baseFolder, Convert.ToHexString(hash, 0, 8));
        Directory.CreateDirectory(folder);

        foreach ((string name, byte[] bytes) in files)
        {
            string path = Path.Combine(folder, name);
            if (File.Exists(path) && new FileInfo(path).Length == bytes.Length)
            {
                continue; // written at an earlier start (and maybe in use by a second window of the program right now)
            }

            // First under a name of its own, then renamed: a second program starting at the same moment never sees half a file.
            string temporary = Path.Combine(folder, name + "." + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }

        return folder;
    }

    /// <summary>The bytes of one of the two files built into the exe.</summary>
    internal static byte[] ReadResource(string fileName)
    {
        using Stream stream = typeof(ScintillaFiles).Assembly.GetManifestResourceStream("native/" + fileName)
            ?? throw new InvalidOperationException("Built-in file not found: " + fileName);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
