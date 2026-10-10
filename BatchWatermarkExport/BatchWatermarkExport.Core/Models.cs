namespace BatchWatermarkExport;

public enum PlatformPreset
{
    YouTubeThumbnail,
    InstagramSquare,
    InstagramPortrait,
    FullResPortfolio
}

public enum WatermarkPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}

public enum ExifPolicy
{
    Keep,
    Strip
}

public sealed record PresetDefinition(
    PlatformPreset Preset,
    int? Width,
    int? Height,
    string Slug,
    string DisplayName);

public sealed record WatermarkOptions(
    string Text,
    WatermarkPosition Position,
    float Opacity,
    float FontSize);

public sealed record ExportRequest(
    string InputDirectory,
    string OutputDirectory,
    WatermarkOptions Watermark,
    ExifPolicy Exif,
    IReadOnlyList<PlatformPreset> Presets,
    DecodeLimits? Limits = null);

public sealed record ExportFileResult(
    string SourcePath,
    string OutputPath,
    PlatformPreset Preset);

public sealed record ExportResult(
    IReadOnlyList<ExportFileResult> Written,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Failed);
