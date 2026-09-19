namespace AuthAnomalyDigester;

/// <summary>Normalized authentication event from any supported log format.</summary>
public sealed class AuthEvent
{
    public DateTime? Timestamp { get; init; }
    /// <summary>success | failure | unknown</summary>
    public string Outcome { get; init; } = "unknown";
    /// <summary>linux_auth | windows_security</summary>
    public string Source { get; init; } = "";
    /// <summary>e.g. ssh_password, ssh_publickey, sudo, logon</summary>
    public string EventType { get; init; } = "";
    public string Username { get; init; } = "";
    public string SourceIp { get; init; } = "";
    public string Raw { get; init; } = "";
    public Dictionary<string, string> Extras { get; init; } = new();

    public Dictionary<string, object?> ToDict()
    {
        var d = new Dictionary<string, object?>
        {
            ["timestamp"] = Timestamp?.ToString("o"),
            ["outcome"] = Outcome,
            ["source"] = Source,
            ["event_type"] = EventType,
            ["username"] = Username,
            ["source_ip"] = SourceIp,
            ["raw"] = Raw,
            ["extras"] = Extras,
        };
        return d;
    }
}

/// <summary>A single anomaly finding produced by a detector.</summary>
public sealed class Finding
{
    public string Detector { get; init; } = "";
    /// <summary>high | medium | low</summary>
    public string Severity { get; init; } = "low";
    public string Summary { get; init; } = "";
    public string Detail { get; init; } = "";
    public int Count { get; init; } = 1;
    public string Username { get; init; } = "";
    public string SourceIp { get; init; } = "";
    public DateTime? FirstSeen { get; init; }
    public DateTime? LastSeen { get; init; }
    public int RelatedEvents { get; init; }

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
        {
            ["detector"] = Detector,
            ["severity"] = Severity,
            ["summary"] = Summary,
            ["detail"] = Detail,
            ["count"] = Count,
            ["username"] = Username,
            ["source_ip"] = SourceIp,
            ["first_seen"] = FirstSeen?.ToString("o"),
            ["last_seen"] = LastSeen?.ToString("o"),
            ["related_events"] = RelatedEvents,
        };
    }
}

/// <summary>Full digester run report.</summary>
public sealed class DigestReport
{
    public List<AuthEvent> Events { get; init; } = new();
    public List<Finding> Findings { get; init; } = new();
    public List<string> InputFiles { get; init; } = new();
    public string FormatUsed { get; init; } = "";
    public Dictionary<string, object?> Config { get; init; } = new();

    public int SuccessCount => Events.Count(e => e.Outcome == "success");
    public int FailureCount => Events.Count(e => e.Outcome == "failure");

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
        {
            ["banner"] =
                "READ-ONLY analysis of files you provide. " +
                "Authorized use only. No live network attacks.",
            ["input_files"] = InputFiles,
            ["format_used"] = FormatUsed,
            ["config"] = Config,
            ["event_count"] = Events.Count,
            ["success_count"] = SuccessCount,
            ["failure_count"] = FailureCount,
            ["finding_count"] = Findings.Count,
            ["findings"] = Findings.Select(f => f.ToDict()).ToList(),
            ["events"] = Events.Select(e => e.ToDict()).ToList(),
        };
    }
}
