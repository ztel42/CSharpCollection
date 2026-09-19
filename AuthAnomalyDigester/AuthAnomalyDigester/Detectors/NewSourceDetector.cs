namespace AuthAnomalyDigester.Detectors;

/// <summary>New source IP detection against a baseline or first-seen-in-file.</summary>
public static class NewSourceDetector
{
    public static HashSet<string> LoadBaselineIps(string path)
    {
        var ips = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;
            var token = line.Split(',')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(token))
                ips.Add(token);
        }
        return ips;
    }

    public static List<Finding> Detect(
        IReadOnlyList<AuthEvent> events,
        HashSet<string>? baseline = null)
    {
        var findings = new List<Finding>();

        if (baseline is not null)
        {
            var seenReport = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ev in events)
            {
                if (ev.Outcome != "success" || string.IsNullOrEmpty(ev.SourceIp)) continue;
                if (baseline.Contains(ev.SourceIp)) continue;
                if (!seenReport.Add(ev.SourceIp)) continue;

                findings.Add(new Finding
                {
                    Detector = "new_source_ip",
                    Severity = "medium",
                    Summary = $"New source IP {ev.SourceIp} (not in baseline)",
                    Detail =
                        $"Successful {ev.EventType} for " +
                        $"{ev.Username.OrUnknown()} from " +
                        $"{ev.SourceIp}, which is absent from the baseline.",
                    Count = 1,
                    Username = ev.Username,
                    SourceIp = ev.SourceIp,
                    FirstSeen = ev.Timestamp,
                    LastSeen = ev.Timestamp,
                    RelatedEvents = 1,
                });
            }
            return findings;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ev in events)
        {
            if (ev.Outcome != "success" || string.IsNullOrEmpty(ev.SourceIp)) continue;
            if (!seen.Add(ev.SourceIp)) continue;

            findings.Add(new Finding
            {
                Detector = "new_source_ip",
                Severity = "low",
                Summary = $"First-seen source IP {ev.SourceIp} in this file",
                Detail =
                    $"No --baseline provided. First successful {ev.EventType} " +
                    $"for {ev.Username.OrUnknown()} from {ev.SourceIp} " +
                    $"in the analyzed file(s).",
                Count = 1,
                Username = ev.Username,
                SourceIp = ev.SourceIp,
                FirstSeen = ev.Timestamp,
                LastSeen = ev.Timestamp,
                RelatedEvents = 1,
            });
        }
        return findings;
    }
}
