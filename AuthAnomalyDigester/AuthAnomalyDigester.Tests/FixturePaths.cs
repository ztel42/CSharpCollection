namespace AuthAnomalyDigester.Tests;

internal static class FixturePaths
{
    public static string Dir
    {
        get
        {
            // Prefer output-copied Fixtures/, fall back to source tree for IDE runs
            var fromOutput = Path.Combine(AppContext.BaseDirectory, "Fixtures");
            if (Directory.Exists(fromOutput))
                return fromOutput;
            var fromCwd = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
            return fromCwd;
        }
    }

    public static string AuthLog => Path.Combine(Dir, "auth.log");
    public static string WindowsCsv => Path.Combine(Dir, "windows_security.csv");
    public static string WindowsXml => Path.Combine(Dir, "windows_security.xml");
    public static string BaselineIps => Path.Combine(Dir, "baseline_ips.txt");
}
