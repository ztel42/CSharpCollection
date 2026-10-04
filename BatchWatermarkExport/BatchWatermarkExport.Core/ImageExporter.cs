using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BatchWatermarkExport;

public static class ImageExporter
{
    /// <summary>
    /// Auto-orients, cover-crops (unless full-res), then stamps the watermark.
    /// Watermark is applied after resize so its size matches the export canvas.
    /// </summary>
    public static void Apply(Image<Rgba32> image, PresetDefinition preset, WatermarkOptions watermark, ExifPolicy exif)
    {
        var kept = exif == ExifPolicy.Keep ? CloneExif(image.Metadata.ExifProfile) : null;

        image.Mutate(ctx => ctx.AutoOrient());

        if (preset.Width is int width && preset.Height is int height)
        {
            ImageResizer.CoverCrop(image, width, height);
        }

        WatermarkRenderer.Apply(image, watermark);

        if (exif == ExifPolicy.Strip)
        {
            image.Metadata.ExifProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;
            return;
        }

        if (kept is null)
        {
            return;
        }

        kept.SetValue(ExifTag.Orientation, (ushort)1);
        UpdateDimensionTags(kept, image.Width, image.Height);
        image.Metadata.ExifProfile = kept;
    }

    private static ExifProfile? CloneExif(ExifProfile? profile)
    {
        if (profile is null)
        {
            return null;
        }

        var bytes = profile.ToByteArray();
        return bytes is { Length: > 0 } ? new ExifProfile(bytes) : new ExifProfile();
    }

    private static void UpdateDimensionTags(ExifProfile profile, int width, int height)
    {
        if (profile.TryGetValue(ExifTag.PixelXDimension, out IExifValue<Number>? _))
        {
            profile.SetValue(ExifTag.PixelXDimension, new Number((uint)width));
        }

        if (profile.TryGetValue(ExifTag.PixelYDimension, out IExifValue<Number>? _))
        {
            profile.SetValue(ExifTag.PixelYDimension, new Number((uint)height));
        }
    }
}
