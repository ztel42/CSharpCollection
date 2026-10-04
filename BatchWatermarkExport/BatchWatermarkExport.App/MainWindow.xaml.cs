using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BatchWatermarkExport;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFolder("Select the folder of stills");
        if (path is not null)
        {
            InputBox.Text = path;
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFolder("Select the output folder");
        if (path is not null)
        {
            OutputBox.Text = path;
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityLabel is null)
        {
            return;
        }

        OpacityLabel.Text = $"{(int)Math.Round(e.NewValue)}%";
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildRequest(out var request, out var error))
        {
            StatusText.Text = error;
            return;
        }

        ExportButton.IsEnabled = false;
        StatusText.Text = "Exporting…";
        try
        {
            var result = await Task.Run(() => BatchExporter.Export(request));
            StatusText.Text =
                $"Wrote {result.Written.Count}, skipped {result.Skipped.Count}, failed {result.Failed.Count}. " +
                "Watermark is applied after cover-crop. Sources were not modified.";
            if (result.Failed.Count > 0)
            {
                StatusText.Text += " " + string.Join(" ", result.Failed);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            ExportButton.IsEnabled = true;
        }
    }

    private bool TryBuildRequest(out ExportRequest request, out string error)
    {
        request = new ExportRequest("", "", new WatermarkOptions("", WatermarkPosition.BottomRight, 0.45f, 0), ExifPolicy.Strip, []);
        error = "";

        var input = InputBox.Text.Trim();
        var output = OutputBox.Text.Trim();
        if (input.Length == 0 || !Directory.Exists(input))
        {
            error = "Choose an input folder that exists.";
            return false;
        }

        if (output.Length == 0)
        {
            error = "Choose an output folder. It must be outside the input folder.";
            return false;
        }

        if (PositionBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag
            || !Enum.TryParse(tag, out WatermarkPosition position))
        {
            error = "Choose a watermark position.";
            return false;
        }

        if (!float.TryParse(FontSizeBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize)
            && !float.TryParse(FontSizeBox.Text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out fontSize))
        {
            error = "Font size must be a number. Use 0 for automatic.";
            return false;
        }

        var presets = new List<PlatformPreset>();
        if (CbYoutube.IsChecked == true) presets.Add(PlatformPreset.YouTubeThumbnail);
        if (CbSquare.IsChecked == true) presets.Add(PlatformPreset.InstagramSquare);
        if (CbPortrait.IsChecked == true) presets.Add(PlatformPreset.InstagramPortrait);
        if (CbFull.IsChecked == true) presets.Add(PlatformPreset.FullResPortfolio);
        if (presets.Count == 0)
        {
            error = "Select at least one platform preset.";
            return false;
        }

        var opacity = (float)(OpacitySlider.Value / 100.0);
        var exif = RbKeep.IsChecked == true ? ExifPolicy.Keep : ExifPolicy.Strip;
        request = new ExportRequest(
            input,
            output,
            new WatermarkOptions(WatermarkBox.Text, position, opacity, fontSize),
            exif,
            presets);
        return true;
    }

    private static string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
