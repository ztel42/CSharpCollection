namespace AuthAnomalyDigester.Parsers;

/// <summary>Format detection and multi-file parse orchestration.</summary>
public static class LogParser
{
    public static string DetectFormat(string text, string? path = null)
    {
        var name = path is null ? "" : Path.GetFileName(path).ToLowerInvariant();
        var sample = text.Length > 4000 ? text[..4000].ToLowerInvariant() : text.ToLowerInvariant();
        var sampleNoSpace = sample.Replace(" ", "");

        if (name.EndsWith(".csv") ||
            (sample.Contains(',') && sample.Contains("event") && sample.Contains("id")))
        {
            if (sample.Contains("4624") || sample.Contains("4625") ||
                sampleNoSpace.Contains("eventid"))
                return "windows";
        }

        if ((name.EndsWith(".xml") || name.EndsWith(".evtx.txt") || name.EndsWith(".txt")) &&
            (sample.Contains("4624") || sample.Contains("4625") || sample.Contains("<event")))
            return "windows";

        if (sample.Contains("sshd") || sample.Contains("sudo:") || sample.Contains("failed password"))
            return "linux";
        if (sample.Contains("accepted password") || sample.Contains("accepted publickey"))
            return "linux";
        if (sample.Contains("authentication failure"))
            return "linux";

        if (name.Contains("auth.log") || name == "secure" || name.Contains("secure"))
            return "linux";
        if (name.Contains("security") && (name.EndsWith(".csv") || name.Contains("export")))
            return "windows";

        return "linux";
    }

    public static (List<AuthEvent> Events, string Format) ParseFile(
        string path, string? formatHint = null)
    {
        var text = File.ReadAllText(path);
        var fmt = formatHint ?? DetectFormat(text, path);
        if (fmt is "windows" or "win" or "csv" or "xml")
            return (WindowsSecurityParser.Parse(text, path), "windows");
        return (LinuxAuthParser.Parse(text, path), "linux");
    }

    public static (List<AuthEvent> Events, string Format) ParseFiles(
        IEnumerable<string> paths, string? formatHint = null)
    {
        var allEvents = new List<AuthEvent>();
        var formats = new List<string>();
        foreach (var p in paths)
        {
            var (events, fmt) = ParseFile(p, formatHint);
            allEvents.AddRange(events);
            formats.Add(fmt);
        }

        var used = formatHint ?? (formats.Count > 0 ? formats[0] : "linux");
        if (formatHint is null && formats.Count > 0)
        {
            used = formats[0];
            var distinct = formats.Distinct().OrderBy(x => x).ToList();
            if (distinct.Count > 1)
                used = "mixed:" + string.Join(",", distinct);
        }
        return (allEvents, used);
    }
}
