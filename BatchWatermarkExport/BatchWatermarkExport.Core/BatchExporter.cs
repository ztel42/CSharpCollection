using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace BatchWatermarkExport;

public static class BatchExporter
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    public static ExportResult Export(ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Watermark);

        if (string.IsNullOrWhiteSpace(request.InputDirectory))
        {
            throw new ArgumentException("Input folder is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            throw new ArgumentException("Output folder is required.", nameof(request));
        }

        if (request.Presets is null || request.Presets.Count == 0)
        {
            throw new ArgumentException("Select at least one platform preset.", nameof(request));
        }

        if (request.Watermark.Opacity < 0f || request.Watermark.Opacity > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Opacity must be between 0 and 1.");
        }

        if (request.Watermark.FontSize < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Font size cannot be negative. Use 0 for automatic size.");
        }

        var inputFull = Path.GetFullPath(request.InputDirectory);
        var outputFull = Path.GetFullPath(request.OutputDirectory);
        if (!Directory.Exists(inputFull))
        {
            throw new DirectoryNotFoundException($"Input folder was not found: {inputFull}");
        }

        if (IsSameOrChild(outputFull, inputFull))
        {
            throw new ArgumentException(
                "Output folder must be outside the input folder so sources are never overwritten or reprocessed.");
        }

        Directory.CreateDirectory(outputFull);

        var written = new List<ExportFileResult>();
        var skipped = new List<string>();
        var failed = new List<string>();
        var presets = request.Presets.Distinct().Select(PlatformPresets.Get).ToList();

        foreach (var source in Directory.EnumerateFiles(inputFull))
        {
            var extension = Path.GetExtension(source);
            if (!ImageExtensions.Contains(extension))
            {
                skipped.Add(source);
                continue;
            }

            try
            {
                var sourceBytesNote = source;
                using var loaded = SafeImageLoader.Load(source, request.Limits);
                foreach (var preset in presets)
                {
                    var destName = Path.GetFileNameWithoutExtension(source) + "__" + preset.Slug + extension;
                    var dest = Path.Combine(outputFull, destName);
                    if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                    {
                        failed.Add($"{sourceBytesNote}: refused to overwrite the source file");
                        continue;
                    }

                    using var working = loaded.Clone();
                    ImageExporter.Apply(working, preset, request.Watermark, request.Exif);
                    Save(working, dest, extension);
                    written.Add(new ExportFileResult(source, dest, preset.Preset));
                }
            }
            catch (Exception ex) when (ex is ImageRejectedException or UnknownImageFormatException or InvalidImageContentException or NotSupportedException or IOException)
            {
                failed.Add($"{source}: {ex.Message}");
            }
        }

        return new ExportResult(written, skipped, failed);
    }

    private static void Save(Image<Rgba32> image, string dest, string extension)
    {
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            image.SaveAsPng(dest);
            return;
        }

        image.SaveAsJpeg(dest, new JpegEncoder { Quality = 90 });
    }

    private static bool IsSameOrChild(string candidate, string parent)
    {
        var parentFull = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidateFull = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(parentFull, candidateFull, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = parentFull + Path.DirectorySeparatorChar;
        return candidateFull.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
