namespace AuthAnomalyDigester.Detectors;

/// <summary>Run all anomaly detectors and return combined findings.</summary>
public static class DetectorRunner
{
    public static List<Finding> Run(
        IReadOnlyList<AuthEvent> events,
        int bfThreshold = 10,
        int bfWindowMinutes = 10,
        int oddStartHour = 7,
        int oddEndHour = 21,
        string? baselinePath = null)
    {
        var findings = new List<Finding>();
        findings.AddRange(BruteForceDetector.Detect(events, bfThreshold, bfWindowMinutes));
        findings.AddRange(OddHoursDetector.Detect(events, oddStartHour, oddEndHour));

        HashSet<string>? baseline = null;
        if (baselinePath is not null)
            baseline = NewSourceDetector.LoadBaselineIps(baselinePath);
        findings.AddRange(NewSourceDetector.Detect(events, baseline));

        var sevOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["high"] = 0,
            ["medium"] = 1,
            ["low"] = 2,
        };
        findings.Sort((a, b) =>
        {
            var sa = sevOrder.GetValueOrDefault(a.Severity, 9);
            var sb = sevOrder.GetValueOrDefault(b.Severity, 9);
            var c = sa.CompareTo(sb);
            if (c != 0) return c;
            c = string.CompareOrdinal(a.Detector, b.Detector);
            if (c != 0) return c;
            return string.CompareOrdinal(a.Summary, b.Summary);
        });
        return findings;
    }
}
