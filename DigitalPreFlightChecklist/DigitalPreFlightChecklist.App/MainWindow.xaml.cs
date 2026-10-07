using System.Windows;

namespace DigitalPreFlightChecklist;

public partial class MainWindow : Window
{
    private readonly ChecklistService _service = new();

    public MainWindow()
    {
        InitializeComponent();
        LogsHint.Text = $"Logs folder: {_service.DefaultLogsDirectory}";
        UpdateCompleteEnabled();
    }

    private void ItemChanged(object sender, RoutedEventArgs e) => UpdateCompleteEnabled();

    private void UpdateCompleteEnabled()
    {
        var ready = CbProps.IsChecked == true
                    && CbFirmware.IsChecked == true
                    && CbBatteries.IsChecked == true
                    && CbLaanc.IsChecked == true
                    && CbRemoteId.IsChecked == true;
        CompleteButton.IsEnabled = ready;
        StatusText.Text = ready
            ? "All five items checked. Complete / Save will write a new timestamped log."
            : "Check all five items to enable Complete / Save.";
    }

    private void Complete_Click(object sender, RoutedEventArgs e)
    {
        var draft = new ChecklistDraft
        {
            Aircraft = AircraftBox.Text,
            Site = SiteBox.Text,
            Notes = NotesBox.Text
        };
        draft.Checked[ChecklistItems.Props] = CbProps.IsChecked == true;
        draft.Checked[ChecklistItems.Firmware] = CbFirmware.IsChecked == true;
        draft.Checked[ChecklistItems.Batteries] = CbBatteries.IsChecked == true;
        draft.Checked[ChecklistItems.LaancAuthorization] = CbLaanc.IsChecked == true;
        draft.Checked[ChecklistItems.RemoteId] = CbRemoteId.IsChecked == true;

        try
        {
            var completion = _service.Complete(draft);
            StatusText.Text =
                $"Saved log {System.IO.Path.GetFileName(completion.LogPath)} at {completion.CompletedAtEastern}. " +
                $"Id {completion.CompletionId[..8]}…";
            LogsHint.Text = $"Logs folder: {System.IO.Path.GetDirectoryName(completion.LogPath)}";
        }
        catch (IncompleteChecklistException ex)
        {
            StatusText.Text = ex.Message;
            UpdateCompleteEnabled();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }
}
