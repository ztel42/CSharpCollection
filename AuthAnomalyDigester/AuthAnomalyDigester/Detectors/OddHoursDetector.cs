namespace AuthAnomalyDigester.Detectors;

/// <summary>Odd-hours detection: successful logons outside a local-hour window.</summary>
public static class OddHoursDetector
{
    public static bool OutsideWindow(int hour, int start, int end)
    {
        if (start == end) return false;
        if (start < end)
            return !(start <= hour && hour < end);
        return !(hour >= start || hour < end);
    }

    public static List<Finding> Detect(
        IReadOnlyList<AuthEvent> events,
        int startHour = 7,
        int endHour = 21)
    {
        var findings = new List<Finding>();
        foreach (var ev in events)
        {
            if (ev.Outcome != "success") continue;
            if (ev.Timestamp is null) continue;
            var hour = ev.Timestamp.Value.Hour;
            if (!OutsideWindow(hour, startHour, endHour)) continue;

            findings.Add(new Finding
            {
                Detector = "odd_hours",
                Severity = "medium",
                Summary =
                    $"Successful {ev.EventType} for {ev.Username.OrUnknown()} " +
                    $"at odd hour {hour:D2}:00",
                Detail =
                    $"Success outside configured window " +
                    $"{startHour:D2}:00–{endHour:D2}:00 " +
                    $"(local). source_ip={ev.SourceIp.OrNa()}",
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

internal static class StringDisplayExtensions
{
    public static string OrUnknown(this string s) =>
        string.IsNullOrEmpty(s) ? "(unknown)" : s;

    public static string OrNa(this string s) =>
        string.IsNullOrEmpty(s) ? "n/a" : s;
}
