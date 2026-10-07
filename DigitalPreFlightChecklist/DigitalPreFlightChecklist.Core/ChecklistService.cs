using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalPreFlightChecklist;

public sealed class IncompleteChecklistException : InvalidOperationException
{
    public IncompleteChecklistException(IReadOnlyList<string> missing)
        : base($"Cannot complete checklist. Missing: {string.Join(", ", missing)}")
    {
        MissingItems = missing;
    }

    public IReadOnlyList<string> MissingItems { get; }
}

/// <summary>Validates drafts and writes timestamped completion logs.</summary>
public sealed class ChecklistService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly TimeZoneInfo _eastern;

    public ChecklistService(TimeZoneInfo? eastern = null)
    {
        _eastern = eastern ?? ResolveEastern();
    }

    public string DefaultLogsDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "DigitalPreFlightChecklist", "logs");

    /// <summary>
    /// Completes a checklist only when all five required items are checked.
    /// Writes a new JSON log under <paramref name="logsDirectory"/> and never overwrites prior logs.
    /// </summary>
    public ChecklistCompletion Complete(ChecklistDraft draft, string? logsDirectory = null, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (!draft.IsComplete)
        {
            throw new IncompleteChecklistException(draft.MissingItems);
        }

        var dir = string.IsNullOrWhiteSpace(logsDirectory) ? DefaultLogsDirectory : logsDirectory;
        Directory.CreateDirectory(dir);

        var completedAt = nowUtc ?? DateTimeOffset.UtcNow;
        var completionId = Guid.NewGuid().ToString("N");
        var easternLocal = TimeZoneInfo.ConvertTime(completedAt, _eastern);
        var easternLabel = FormatEastern(easternLocal);

        var items = ChecklistItems.Required.ToDictionary(
            name => name,
            name => draft.Checked.TryGetValue(name, out var ok) && ok,
            StringComparer.Ordinal);

        var stamp = completedAt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var fileName = $"preflight_{stamp}_{completionId[..8]}.json";
        var path = Path.Combine(dir, fileName);

        // Guaranteed unique: if collision somehow exists, append another id fragment.
        var attempt = 0;
        while (File.Exists(path))
        {
            attempt++;
            fileName = $"preflight_{stamp}_{completionId[..8]}_{attempt}.json";
            path = Path.Combine(dir, fileName);
        }

        var payload = new LogPayload
        {
            CompletionId = completionId,
            CompletedAtUtc = completedAt.UtcDateTime.ToString("o", CultureInfo.InvariantCulture),
            CompletedAtEastern = easternLabel,
            Aircraft = draft.Aircraft?.Trim() ?? "",
            Site = draft.Site?.Trim() ?? "",
            Notes = draft.Notes?.Trim() ?? "",
            Items = items
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(path, json);

        return new ChecklistCompletion
        {
            CompletionId = completionId,
            CompletedAtUtc = completedAt.ToUniversalTime(),
            CompletedAtEastern = easternLabel,
            Aircraft = payload.Aircraft,
            Site = payload.Site,
            Notes = payload.Notes,
            Items = items,
            LogPath = path
        };
    }

    public static ChecklistCompletion? TryReadLog(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var payload = JsonSerializer.Deserialize<LogPayload>(File.ReadAllText(path), JsonOptions);
        if (payload is null || string.IsNullOrWhiteSpace(payload.CompletionId))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(payload.CompletedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var utc))
        {
            utc = DateTimeOffset.UnixEpoch;
        }

        return new ChecklistCompletion
        {
            CompletionId = payload.CompletionId,
            CompletedAtUtc = utc.ToUniversalTime(),
            CompletedAtEastern = payload.CompletedAtEastern ?? "",
            Aircraft = payload.Aircraft ?? "",
            Site = payload.Site ?? "",
            Notes = payload.Notes ?? "",
            Items = payload.Items ?? new Dictionary<string, bool>(),
            LogPath = path
        };
    }

    private static string FormatEastern(DateTimeOffset easternLocal)
    {
        // Label as ET (America/New_York); offset already reflects EST/EDT.
        var offset = easternLocal.ToString("zzz", CultureInfo.InvariantCulture);
        return $"{easternLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} ET ({offset})";
    }

    private static TimeZoneInfo ResolveEastern()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }

    private sealed class LogPayload
    {
        public string CompletionId { get; set; } = "";
        public string CompletedAtUtc { get; set; } = "";
        public string CompletedAtEastern { get; set; } = "";
        public string Aircraft { get; set; } = "";
        public string Site { get; set; } = "";
        public string Notes { get; set; } = "";
        public Dictionary<string, bool> Items { get; set; } = new();
    }
}
