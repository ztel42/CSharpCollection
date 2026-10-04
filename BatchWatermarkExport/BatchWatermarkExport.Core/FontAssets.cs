using System.Reflection;
using SixLabors.Fonts;

namespace BatchWatermarkExport;

internal static class FontAssets
{
    private const string ResourceName = "BatchWatermarkExport.Fonts.LiberationSans-Regular.ttf";

    private static readonly FontCollection Collection = new();
    private static readonly FontFamily Family = Load();

    public static Font Create(float size) => Family.CreateFont(size, FontStyle.Regular);

    private static FontFamily Load()
    {
        var assembly = typeof(FontAssets).Assembly;
        using var resource = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded font '{ResourceName}' was not found.");

        // Fonts 2 may retain the stream; keep a copy alive for the process.
        var buffer = new MemoryStream();
        resource.CopyTo(buffer);
        buffer.Position = 0;
        return Collection.Add(buffer);
    }
}
