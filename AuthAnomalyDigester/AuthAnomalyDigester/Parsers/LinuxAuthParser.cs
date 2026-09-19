using System.Globalization;
using System.Text.RegularExpressions;

namespace AuthAnomalyDigester.Parsers;

/// <summary>Parser for Linux auth.log / syslog-style SSH and sudo lines.</summary>
public static class LinuxAuthParser
{
    private static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jan"] = 1, ["feb"] = 2, ["mar"] = 3, ["apr"] = 4,
        ["may"] = 5, ["jun"] = 6, ["jul"] = 7, ["aug"] = 8,
        ["sep"] = 9, ["oct"] = 10, ["nov"] = 11, ["dec"] = 12,
    };

    private static readonly Regex TsSyslog = new(
        @"^(?<mon>Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+" +
        @"(?<day>\d{1,2})\s+" +
        @"(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TsIso = new(
        @"^(?<iso>\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?)",
        RegexOptions.Compiled);

    private static readonly Regex SshFail = new(
        @"Failed password for (?:invalid user )?(?<user>\S+)\s+from\s+(?<ip>\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SshAccept = new(
        @"Accepted (?<method>password|publickey) for (?<user>\S+)\s+from\s+(?<ip>\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SudoFail = new(
        @"sudo:.*authentication failure.*(?:ruser=(?<ruser>\S*))?.*user=(?<user>\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SudoOk = new(
        @"sudo:\s+(?<user>\S+)\s+:\s+TTY=.*USER=(?<target>\S+)\s*;\s*COMMAND=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AuthFailGeneric = new(
        @"authentication failure.*(?:rhost=(?<ip>\S*))?.*user=(?<user>\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static DateTime? ParseTimestamp(string line, int defaultYear = 2026)
    {
        var m = TsIso.Match(line);
        if (m.Success)
        {
            var raw = m.Groups["iso"].Value.Replace("Z", "+00:00");
            if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var dto))
                return dto.DateTime;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var dt))
                return dt;
        }

        m = TsSyslog.Match(line);
        if (!m.Success) return null;

        var mon = Months[m.Groups["mon"].Value.ToLowerInvariant()];
        var day = int.Parse(m.Groups["day"].Value, CultureInfo.InvariantCulture);
        var h = int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
        var mi = int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture);
        var s = int.Parse(m.Groups["s"].Value, CultureInfo.InvariantCulture);
        try
        {
            return new DateTime(defaultYear, mon, day, h, mi, s, DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    public static List<AuthEvent> Parse(string text, string sourcePath = "", int defaultYear = 2026)
    {
        var events = new List<AuthEvent>();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var ts = ParseTimestamp(trimmed, defaultYear);
            var lower = trimmed.ToLowerInvariant();

            var m = SshFail.Match(trimmed);
            if (m.Success)
            {
                events.Add(new AuthEvent
                {
                    Timestamp = ts,
                    Outcome = "failure",
                    Source = "linux_auth",
                    EventType = "ssh_password",
                    Username = m.Groups["user"].Value,
                    SourceIp = m.Groups["ip"].Value,
                    Raw = trimmed.TrimEnd(),
                    Extras = new Dictionary<string, string> { ["path"] = sourcePath },
                });
                continue;
            }

            m = SshAccept.Match(trimmed);
            if (m.Success)
            {
                var method = m.Groups["method"].Value.ToLowerInvariant();
                events.Add(new AuthEvent
                {
                    Timestamp = ts,
                    Outcome = "success",
                    Source = "linux_auth",
                    EventType = $"ssh_{method}",
                    Username = m.Groups["user"].Value,
                    SourceIp = m.Groups["ip"].Value,
                    Raw = trimmed.TrimEnd(),
                    Extras = new Dictionary<string, string>
                    {
                        ["path"] = sourcePath,
                        ["method"] = method,
                    },
                });
                continue;
            }

            if (lower.Contains("sudo:") && lower.Contains("authentication failure"))
            {
                m = SudoFail.Match(trimmed);
                if (!m.Success) m = AuthFailGeneric.Match(trimmed);
                var user = "";
                if (m.Success)
                {
                    user = m.Groups["user"].Success && !string.IsNullOrEmpty(m.Groups["user"].Value)
                        ? m.Groups["user"].Value
                        : (m.Groups["ruser"].Success ? m.Groups["ruser"].Value : "");
                }
                events.Add(new AuthEvent
                {
                    Timestamp = ts,
                    Outcome = "failure",
                    Source = "linux_auth",
                    EventType = "sudo",
                    Username = user,
                    SourceIp = "",
                    Raw = trimmed.TrimEnd(),
                    Extras = new Dictionary<string, string> { ["path"] = sourcePath },
                });
                continue;
            }

            m = SudoOk.Match(trimmed);
            if (m.Success && !lower.Contains("authentication failure"))
            {
                events.Add(new AuthEvent
                {
                    Timestamp = ts,
                    Outcome = "success",
                    Source = "linux_auth",
                    EventType = "sudo",
                    Username = m.Groups["user"].Value,
                    SourceIp = "",
                    Raw = trimmed.TrimEnd(),
                    Extras = new Dictionary<string, string>
                    {
                        ["path"] = sourcePath,
                        ["target_user"] = m.Groups["target"].Value,
                    },
                });
                continue;
            }

            if (lower.Contains("authentication failure") && lower.Contains("sshd"))
            {
                m = AuthFailGeneric.Match(trimmed);
                if (m.Success)
                {
                    events.Add(new AuthEvent
                    {
                        Timestamp = ts,
                        Outcome = "failure",
                        Source = "linux_auth",
                        EventType = "ssh_auth",
                        Username = m.Groups["user"].Success ? m.Groups["user"].Value : "",
                        SourceIp = m.Groups["ip"].Success
                            ? m.Groups["ip"].Value.TrimEnd()
                            : "",
                        Raw = trimmed.TrimEnd(),
                        Extras = new Dictionary<string, string> { ["path"] = sourcePath },
                    });
                }
            }
        }

        return events;
    }
}
