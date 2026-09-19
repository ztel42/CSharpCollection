using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuthAnomalyDigester;

/// <summary>Console, JSON, and CSV report writers.</summary>
public static class ReportWriter
{
    public const string Banner =
        "========================================================================\n" +
        "  READ-ONLY ANALYSIS OF FILES YOU PROVIDE  |  AUTHORIZED USE ONLY\n" +
        "  No live network attacks. No credential guessing. Portfolio / IR hygiene.\n" +
        "========================================================================";

    public static void PrintBanner(TextWriter? outWriter = null)
    {
        (outWriter ?? Console.Out).WriteLine(Banner);
    }

    public static void PrintSummary(DigestReport report, TextWriter? outWriter = null)
    {
        var o = outWriter ?? Console.Out;
        o.WriteLine();
        o.WriteLine("Auth Anomaly Digester — summary");
        o.WriteLine($"  Format       : {report.FormatUsed}");
        o.WriteLine($"  Input files  : {report.InputFiles.Count}");
        foreach (var p in report.InputFiles)
            o.WriteLine($"    - {p}");
        o.WriteLine($"  Events       : {report.Events.Count}");
        o.WriteLine($"    - success  : {report.SuccessCount}");
        o.WriteLine($"    - failure  : {report.FailureCount}");
        o.WriteLine($"  Findings     : {report.Findings.Count}");

        var bySev = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byDet = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in report.Findings)
        {
            bySev[f.Severity] = bySev.GetValueOrDefault(f.Severity) + 1;
            byDet[f.Detector] = byDet.GetValueOrDefault(f.Detector) + 1;
        }
        foreach (var sev in new[] { "high", "medium", "low" })
        {
            if (bySev.TryGetValue(sev, out var n))
                o.WriteLine($"    - {sev}: {n}");
        }
        if (byDet.Count > 0)
        {
            o.WriteLine("  By detector  :");
            foreach (var (det, n) in byDet.OrderBy(kv => kv.Key))
                o.WriteLine($"    - {det}: {n}");
        }

        if (report.Findings.Count > 0)
        {
            o.WriteLine();
            o.WriteLine("  Top findings:");
            foreach (var f in report.Findings.Take(20))
                o.WriteLine($"    [{f.Severity}] {f.Detector}: {f.Summary}");
            if (report.Findings.Count > 20)
                o.WriteLine($"    ... and {report.Findings.Count - 20} more");
        }
        else
        {
            o.WriteLine();
            o.WriteLine("  No anomalies flagged with current thresholds.");
        }
        o.WriteLine();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = null,
    };

    public static void WriteJson(DigestReport report, string path)
    {
        var json = JsonSerializer.Serialize(report.ToDict(), JsonOpts);
        File.WriteAllText(path, json + "\n", Encoding.UTF8);
    }

    public static void WriteCsv(DigestReport report, string path)
    {
        var fieldnames = new[]
        {
            "detector", "severity", "summary", "detail", "count",
            "username", "source_ip", "first_seen", "last_seen", "related_events",
        };
        using var sw = new StreamWriter(path, false, new UTF8Encoding(false));
        sw.WriteLine(string.Join(",", fieldnames));
        foreach (var f in report.Findings)
        {
            var row = f.ToDict();
            var cells = fieldnames.Select(k =>
            {
                row.TryGetValue(k, out var v);
                return CsvEscape(v?.ToString() ?? "");
            });
            sw.WriteLine(string.Join(",", cells));
        }
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') ||
            value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
