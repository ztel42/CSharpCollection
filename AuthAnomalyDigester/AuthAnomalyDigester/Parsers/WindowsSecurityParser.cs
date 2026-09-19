using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AuthAnomalyDigester.Parsers;

/// <summary>
/// Parser for exported Windows Security events (CSV / simple XML text).
/// Binary .evtx is not parsed — export to CSV or XML text first.
/// Supports Event IDs 4624 and 4625.
/// </summary>
public static class WindowsSecurityParser
{
    private static readonly Regex EventIdRe = new(@"\b(4624|4625)\b", RegexOptions.Compiled);

    private static readonly string[] DtFormats =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fffffff",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "MM/dd/yyyy HH:mm:ss",
        "MM/dd/yyyy hh:mm:ss tt",
        "dd/MM/yyyy HH:mm:ss",
    };

    public static DateTime? ParseDt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim().Trim('"');
        var cleaned = value.Replace("Z", "", StringComparison.Ordinal);
        foreach (var fmt in DtFormats)
        {
            if (DateTime.TryParseExact(cleaned, fmt, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;
        }
        if (DateTimeOffset.TryParse(value.Replace("Z", "+00:00"), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dto))
            return dto.DateTime;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsed))
            return parsed;
        return null;
    }

    private static string NormKey(string k) =>
        Regex.Replace(k.ToLowerInvariant(), @"[^a-z0-9]", "");

    private static string RowGet(Dictionary<string, string> row, params string[] candidates)
    {
        var norm = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in row)
        {
            if (kv.Key is null) continue;
            norm[NormKey(kv.Key)] = kv.Value ?? "";
        }
        foreach (var c in candidates)
        {
            if (norm.TryGetValue(NormKey(c), out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }
        return "";
    }

    public static bool IsCsv(string text)
    {
        string? first = null;
        foreach (var line in text.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                first = line.TrimEnd('\r');
                break;
            }
        }
        if (first is null) return false;
        var lower = first.ToLowerInvariant();
        return (first.Contains(',') || first.Contains(';')) &&
               (lower.Contains("event") || lower.Contains("time") ||
                lower.Contains("id") || lower.Contains("account"));
    }

    public static bool IsXml(string text)
    {
        var s = text.TrimStart();
        return s.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
               s.StartsWith("<Events", StringComparison.OrdinalIgnoreCase) ||
               s.StartsWith("<Event", StringComparison.OrdinalIgnoreCase);
    }

    public static List<AuthEvent> ParseCsv(string text, string sourcePath = "")
    {
        var sample = text.TrimStart('\uFEFF');
        var first = sample.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
        var delim = first.Count(c => c == ';') > first.Count(c => c == ',') ? ';' : ',';

        var lines = sample.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        if (lines.Count < 2) return new List<AuthEvent>();

        var headers = SplitCsvLine(lines[0], delim);
        var events = new List<AuthEvent>();

        for (var i = 1; i < lines.Count; i++)
        {
            var cols = SplitCsvLine(lines[i], delim);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                var key = headers[c];
                var val = c < cols.Count ? cols[c] : "";
                row[key] = val;
            }

            var eid = RowGet(row, "EventID", "Event Id", "Id", "EventID");
            if (string.IsNullOrEmpty(eid))
            {
                var joined = string.Join(" ", row.Values);
                var m = EventIdRe.Match(joined);
                eid = m.Success ? m.Groups[1].Value : "";
            }
            if (eid is not ("4624" or "4625")) continue;

            var outcome = eid == "4624" ? "success" : "failure";
            var ts = ParseDt(RowGet(row,
                "TimeCreated", "Time Generated", "Date and Time",
                "Timestamp", "Time", "SystemTime"));
            var user = RowGet(row,
                "TargetUserName", "Account Name", "AccountName",
                "User", "Username", "SubjectUserName");
            var ip = RowGet(row,
                "IpAddress", "Source Network Address", "Source Address",
                "ClientAddress", "Workstation Name", "Ip Address");
            if (ip == "-") ip = "";

            events.Add(new AuthEvent
            {
                Timestamp = ts,
                Outcome = outcome,
                Source = "windows_security",
                EventType = "logon",
                Username = user,
                SourceIp = ip,
                Raw = string.Join(",", row.Select(kv => $"{kv.Key}={kv.Value}")),
                Extras = new Dictionary<string, string>
                {
                    ["path"] = sourcePath,
                    ["event_id"] = eid,
                },
            });
        }

        return events;
    }

    private static List<string> SplitCsvLine(string line, char delim)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == delim && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    public static List<AuthEvent> ParseXml(string text, string sourcePath = "")
    {
        var events = new List<AuthEvent>();
        var body = text.Trim();
        if (!body.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
            !body.StartsWith("<Events", StringComparison.OrdinalIgnoreCase))
        {
            if (body.Contains("<Event", StringComparison.OrdinalIgnoreCase))
                body = $"<Events>{body}</Events>";
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(body);
        }
        catch (System.Xml.XmlException)
        {
            return ParseTextFallback(text, sourcePath);
        }

        foreach (var ev in doc.Descendants().Where(e => LocalName(e) == "Event"))
        {
            var eid = "";
            DateTime? ts = null;
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var child in ev.Descendants())
            {
                var ctag = LocalName(child);
                if (ctag == "EventID" && child.Value is not null)
                    eid = child.Value.Trim();
                else if (ctag == "TimeCreated")
                {
                    var sysT = (string?)child.Attribute("SystemTime") ?? "";
                    ts = ParseDt(sysT);
                }
                else if (ctag == "Data")
                {
                    var name = (string?)child.Attribute("Name") ?? "";
                    if (!string.IsNullOrEmpty(name))
                        data[name] = child.Value?.Trim() ?? "";
                }
            }

            if (eid is not ("4624" or "4625")) continue;

            var user = data.GetValueOrDefault("TargetUserName")
                       ?? data.GetValueOrDefault("SubjectUserName")
                       ?? "";
            var ip = data.GetValueOrDefault("IpAddress")
                     ?? data.GetValueOrDefault("WorkstationName")
                     ?? "";
            if (ip == "-") ip = "";

            events.Add(new AuthEvent
            {
                Timestamp = ts,
                Outcome = eid == "4624" ? "success" : "failure",
                Source = "windows_security",
                EventType = "logon",
                Username = user,
                SourceIp = ip,
                Raw = ev.ToString(SaveOptions.DisableFormatting),
                Extras = new Dictionary<string, string>
                {
                    ["path"] = sourcePath,
                    ["event_id"] = eid,
                },
            });
        }

        return events;
    }

    private static string LocalName(XElement e) =>
        e.Name.LocalName;

    public static List<AuthEvent> ParseTextFallback(string text, string sourcePath = "")
    {
        var events = new List<AuthEvent>();
        var blocks = Regex.Split(text, @"(?=\bEvent\s*ID\s*[:=]?\s*462[45]\b)",
            RegexOptions.IgnoreCase);
        foreach (var block in blocks)
        {
            var m = EventIdRe.Match(block);
            if (!m.Success) continue;
            var eid = m.Groups[1].Value;

            DateTime? ts = null;
            var tm = Regex.Match(block, @"(\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2})");
            if (tm.Success) ts = ParseDt(tm.Groups[1].Value);

            var userM = Regex.Match(block,
                @"(?:Account Name|TargetUserName|AccountName)\s*[:=]\s*(\S+)",
                RegexOptions.IgnoreCase);
            var ipM = Regex.Match(block,
                @"(?:Source Network Address|IpAddress|Source Address)\s*[:=]\s*(\S+)",
                RegexOptions.IgnoreCase);
            var user = userM.Success ? userM.Groups[1].Value : "";
            var ip = ipM.Success ? ipM.Groups[1].Value : "";
            if (ip == "-") ip = "";

            events.Add(new AuthEvent
            {
                Timestamp = ts,
                Outcome = eid == "4624" ? "success" : "failure",
                Source = "windows_security",
                EventType = "logon",
                Username = user,
                SourceIp = ip,
                Raw = block.Length > 500 ? block[..500].Trim() : block.Trim(),
                Extras = new Dictionary<string, string>
                {
                    ["path"] = sourcePath,
                    ["event_id"] = eid,
                },
            });
        }
        return events;
    }

    public static List<AuthEvent> Parse(string text, string sourcePath = "")
    {
        if (IsCsv(text))
            return ParseCsv(text, sourcePath);
        if (IsXml(text) || text.Contains("<Event", StringComparison.OrdinalIgnoreCase))
            return ParseXml(text, sourcePath);
        if (text.Contains(',') && text.Contains('\n'))
        {
            var ev = ParseCsv(text, sourcePath);
            if (ev.Count > 0) return ev;
        }
        return ParseTextFallback(text, sourcePath);
    }
}
