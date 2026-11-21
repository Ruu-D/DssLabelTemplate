using System.Reflection;

namespace DssLabelTemplate;

/// <summary>Facts about the program itself.</summary>
internal static class AppInfo
{
    /// <summary>
    /// The version as "1.2.1", or "2.0.0-alpha" for a test version. It comes from the &lt;Version&gt; line in
    /// DssLabelTemplate.csproj, so there is one place to change it. (The assembly version itself has room for numbers
    /// only, so the full text is read from the "informational" version, without the "+commit" the build adds to it.)
    /// </summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? full = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(full))
        {
            int plus = full.IndexOf('+');
            return plus >= 0 ? full[..plus] : full;
        }

        return assembly.GetName().Version?.ToString(3) ?? "?";
    }
}
