using AuthAnomalyDigester.Detectors;
using AuthAnomalyDigester.Parsers;

namespace AuthAnomalyDigester;

/// <summary>Core digester orchestration (parity with Python run_digest).</summary>
public static class Digester
{
    public static DigestReport Run(
        IReadOnlyList<string> paths,
        string? formatHint = null,
        string? baseline = null,
        int bfThreshold = 10,
        int bfWindow = 10,
        int oddStart = 7,
        int oddEnd = 21)
    {
        var hint = formatHint is null or "auto" ? null : formatHint;
        var (events, fmt) = LogParser.ParseFiles(paths, hint);
        var findings = DetectorRunner.Run(
            events,
            bfThreshold: bfThreshold,
            bfWindowMinutes: bfWindow,
            oddStartHour: oddStart,
            oddEndHour: oddEnd,
            baselinePath: baseline);

        return new DigestReport
        {
            Events = events,
            Findings = findings,
            InputFiles = paths.ToList(),
            FormatUsed = fmt,
            Config = new Dictionary<string, object?>
            {
                ["bf_threshold"] = bfThreshold,
                ["bf_window_minutes"] = bfWindow,
                ["odd_start_hour"] = oddStart,
                ["odd_end_hour"] = oddEnd,
                ["baseline"] = baseline,
                ["format_hint"] = formatHint ?? "auto",
            },
        };
    }
}
