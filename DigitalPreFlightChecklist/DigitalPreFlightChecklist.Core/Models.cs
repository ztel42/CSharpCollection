namespace DigitalPreFlightChecklist;

/// <summary>The five required pre-flight items.</summary>
public static class ChecklistItems
{
    public const string Props = "Props";
    public const string Firmware = "Firmware";
    public const string Batteries = "Batteries";
    public const string LaancAuthorization = "LAANC authorization";
    public const string RemoteId = "Remote ID";

    public static IReadOnlyList<string> Required { get; } =
    [
        Props,
        Firmware,
        Batteries,
        LaancAuthorization,
        RemoteId
    ];
}

/// <summary>In-progress checklist state before completion.</summary>
public sealed class ChecklistDraft
{
    public string Aircraft { get; set; } = "";
    public string Site { get; set; } = "";
    public string Notes { get; set; } = "";
    public Dictionary<string, bool> Checked { get; set; } = new(StringComparer.Ordinal);

    public ChecklistDraft()
    {
        foreach (var item in ChecklistItems.Required)
        {
            Checked[item] = false;
        }
    }

    public bool IsComplete => ChecklistItems.Required.All(item => Checked.TryGetValue(item, out var ok) && ok);

    public IReadOnlyList<string> MissingItems =>
        ChecklistItems.Required.Where(item => !Checked.TryGetValue(item, out var ok) || !ok).ToList();
}

/// <summary>Immutable record written to a timestamped log file.</summary>
public sealed class ChecklistCompletion
{
    public required string CompletionId { get; init; }
    public required DateTimeOffset CompletedAtUtc { get; init; }
    public required string CompletedAtEastern { get; init; }
    public required string Aircraft { get; init; }
    public required string Site { get; init; }
    public required string Notes { get; init; }
    public required IReadOnlyDictionary<string, bool> Items { get; init; }
    public required string LogPath { get; init; }
}
