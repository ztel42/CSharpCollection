using System.Text.Json;
using DigitalPreFlightChecklist;
using DigitalPreFlightChecklist.Cli;

namespace DigitalPreFlightChecklist.Tests;

public class ChecklistTests
{
    private static ChecklistDraft FullDraft(string aircraft = "Mavic 3", string site = "Space Coast", string notes = "Clear skies")
    {
        var draft = new ChecklistDraft
        {
            Aircraft = aircraft,
            Site = site,
            Notes = notes
        };
        foreach (var item in ChecklistItems.Required)
        {
            draft.Checked[item] = true;
        }
        return draft;
    }

    [Fact]
    public void Cannot_complete_without_all_five_items()
    {
        using var temp = new TempDir();
        var service = new ChecklistService();
        var draft = FullDraft();
        draft.Checked[ChecklistItems.RemoteId] = false;

        var ex = Assert.Throws<IncompleteChecklistException>(() => service.Complete(draft, temp.Path));
        Assert.Contains(ChecklistItems.RemoteId, ex.MissingItems);
        Assert.Empty(Directory.GetFiles(temp.Path));
    }

    [Theory]
    [InlineData("Props")]
    [InlineData("Firmware")]
    [InlineData("Batteries")]
    [InlineData("LAANC authorization")]
    [InlineData("Remote ID")]
    public void Cannot_complete_when_any_single_item_is_unchecked(string missing)
    {
        using var temp = new TempDir();
        var service = new ChecklistService();
        var draft = FullDraft();
        draft.Checked[missing] = false;

        Assert.Throws<IncompleteChecklistException>(() => service.Complete(draft, temp.Path));
        Assert.Empty(Directory.GetFiles(temp.Path));
    }

    [Fact]
    public void Complete_writes_log_with_expected_fields()
    {
        using var temp = new TempDir();
        var service = new ChecklistService();
        var now = new DateTimeOffset(2026, 10, 7, 17, 40, 0, TimeSpan.Zero);
        var completion = service.Complete(FullDraft(), temp.Path, now);

        Assert.True(File.Exists(completion.LogPath));
        Assert.False(string.IsNullOrWhiteSpace(completion.CompletionId));
        Assert.Equal(now.ToUniversalTime(), completion.CompletedAtUtc);
        Assert.Contains("ET", completion.CompletedAtEastern);
        Assert.Equal("Mavic 3", completion.Aircraft);
        Assert.Equal("Space Coast", completion.Site);
        Assert.Equal("Clear skies", completion.Notes);
        Assert.Equal(5, completion.Items.Count);
        Assert.All(ChecklistItems.Required, item => Assert.True(completion.Items[item]));

        using var doc = JsonDocument.Parse(File.ReadAllText(completion.LogPath));
        var root = doc.RootElement;
        Assert.Equal(completion.CompletionId, root.GetProperty("completionId").GetString());
        Assert.NotNull(root.GetProperty("completedAtUtc").GetString());
        Assert.Contains("ET", root.GetProperty("completedAtEastern").GetString());
        Assert.Equal("Mavic 3", root.GetProperty("aircraft").GetString());
        Assert.Equal("Space Coast", root.GetProperty("site").GetString());
        Assert.Equal("Clear skies", root.GetProperty("notes").GetString());
        var items = root.GetProperty("items");
        Assert.True(items.GetProperty("Props").GetBoolean());
        Assert.True(items.GetProperty("Firmware").GetBoolean());
        Assert.True(items.GetProperty("Batteries").GetBoolean());
        Assert.True(items.GetProperty("LAANC authorization").GetBoolean());
        Assert.True(items.GetProperty("Remote ID").GetBoolean());
    }

    [Fact]
    public void Multiple_completes_create_multiple_log_files()
    {
        using var temp = new TempDir();
        var service = new ChecklistService();
        var t1 = new DateTimeOffset(2026, 10, 7, 17, 40, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 10, 7, 17, 41, 0, TimeSpan.Zero);

        var first = service.Complete(FullDraft(notes: "first"), temp.Path, t1);
        var second = service.Complete(FullDraft(notes: "second"), temp.Path, t2);

        Assert.NotEqual(first.CompletionId, second.CompletionId);
        Assert.NotEqual(first.LogPath, second.LogPath);
        Assert.True(File.Exists(first.LogPath));
        Assert.True(File.Exists(second.LogPath));

        var files = Directory.GetFiles(temp.Path, "preflight_*.json");
        Assert.Equal(2, files.Length);

        var reread = ChecklistService.TryReadLog(first.LogPath);
        Assert.NotNull(reread);
        Assert.Equal("first", reread!.Notes);
    }

    [Fact]
    public void Draft_reports_missing_items_until_all_checked()
    {
        var draft = new ChecklistDraft();
        Assert.False(draft.IsComplete);
        Assert.Equal(5, draft.MissingItems.Count);

        foreach (var item in ChecklistItems.Required)
        {
            draft.Checked[item] = true;
        }

        Assert.True(draft.IsComplete);
        Assert.Empty(draft.MissingItems);
    }

    [Fact]
    public void Cli_complete_all_writes_log_and_returns_zero()
    {
        using var temp = new TempDir();
        var code = Program.Main(
        [
            "complete",
            "--all",
            "--aircraft", "Mini 4 Pro",
            "--site", "Field A",
            "--notes", "CLI smoke",
            "--logs", temp.Path
        ]);

        Assert.Equal(0, code);
        var files = Directory.GetFiles(temp.Path, "preflight_*.json");
        Assert.Single(files);

        var log = ChecklistService.TryReadLog(files[0]);
        Assert.NotNull(log);
        Assert.Equal("Mini 4 Pro", log!.Aircraft);
        Assert.Equal("Field A", log.Site);
        Assert.Equal("CLI smoke", log.Notes);
        Assert.All(ChecklistItems.Required, item => Assert.True(log.Items[item]));
    }

    [Fact]
    public void Cli_complete_without_all_items_returns_nonzero_and_writes_nothing()
    {
        using var temp = new TempDir();
        var code = Program.Main(
        [
            "complete",
            "--item", "props",
            "--item", "firmware",
            "--logs", temp.Path
        ]);

        Assert.Equal(1, code);
        Assert.Empty(Directory.GetFiles(temp.Path));
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dpfc-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
