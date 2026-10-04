namespace BatchWatermarkExport;

public static class PlatformPresets
{
    public static IReadOnlyList<PresetDefinition> All { get; } =
    [
        new(PlatformPreset.YouTubeThumbnail, 1280, 720, "youtube-1280x720", "YouTube thumbnail 1280×720"),
        new(PlatformPreset.InstagramSquare, 1080, 1080, "instagram-1080x1080", "Instagram square 1080×1080"),
        new(PlatformPreset.InstagramPortrait, 1080, 1350, "instagram-1080x1350", "Instagram portrait 1080×1350"),
        new(PlatformPreset.FullResPortfolio, null, null, "full-res", "Full-res portfolio (original size)")
    ];

    public static PresetDefinition Get(PlatformPreset preset)
    {
        foreach (var item in All)
        {
            if (item.Preset == preset)
            {
                return item;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown platform preset.");
    }

    public static bool TryParse(string text, out PlatformPreset preset)
    {
        var key = text.Trim().ToLowerInvariant().Replace("_", "-");
        switch (key)
        {
            case "youtube":
            case "youtube-thumbnail":
            case "youtube-1280x720":
                preset = PlatformPreset.YouTubeThumbnail;
                return true;
            case "instagram-square":
            case "ig-square":
            case "instagram-1080x1080":
                preset = PlatformPreset.InstagramSquare;
                return true;
            case "instagram-portrait":
            case "ig-portrait":
            case "instagram-1080x1350":
                preset = PlatformPreset.InstagramPortrait;
                return true;
            case "full-res":
            case "fullres":
            case "portfolio":
            case "full-res-portfolio":
                preset = PlatformPreset.FullResPortfolio;
                return true;
            default:
                preset = default;
                return false;
        }
    }

    public static bool TryParsePosition(string text, out WatermarkPosition position)
    {
        var key = text.Trim().ToLowerInvariant().Replace("_", "-");
        switch (key)
        {
            case "top-left":
            case "tl":
                position = WatermarkPosition.TopLeft;
                return true;
            case "top-right":
            case "tr":
                position = WatermarkPosition.TopRight;
                return true;
            case "bottom-left":
            case "bl":
                position = WatermarkPosition.BottomLeft;
                return true;
            case "bottom-right":
            case "br":
                position = WatermarkPosition.BottomRight;
                return true;
            case "center":
            case "centre":
                position = WatermarkPosition.Center;
                return true;
            default:
                position = default;
                return false;
        }
    }
}
