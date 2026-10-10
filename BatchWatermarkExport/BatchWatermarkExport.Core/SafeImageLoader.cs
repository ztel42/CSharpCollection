using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace BatchWatermarkExport;

/// <summary>
/// Resource limits applied to every source image before it is fully decoded.
/// Defaults: 100 MB file size, 20000 px per side, 100 megapixels.
/// </summary>
public sealed record DecodeLimits(long MaxFileBytes, int MaxWidth, int MaxHeight, long MaxPixels)
{
    /// <summary>Largest source file read, in bytes (100 MB).</summary>
    public const long DefaultMaxFileBytes = 100L * 1024 * 1024;

    /// <summary>Largest width or height accepted, in pixels.</summary>
    public const int DefaultMaxDimension = 20_000;

    /// <summary>Largest total pixel count accepted (100 MP, about 400 MB as Rgba32).</summary>
    public const long DefaultMaxPixels = 100_000_000;

    /// <summary>
    /// Cap on any single ImageSharp buffer allocation, in megabytes. Applied to the
    /// decoding configuration's MemoryAllocator so a lying header cannot request
    /// an unbounded pixel buffer.
    /// </summary>
    public const int AllocationLimitMegabytes = 1024;

    public static DecodeLimits Default { get; } =
        new(DefaultMaxFileBytes, DefaultMaxDimension, DefaultMaxDimension, DefaultMaxPixels);
}

/// <summary>A source file that was refused before a full decode.</summary>
public sealed class ImageRejectedException(string message) : Exception(message);

/// <summary>
/// Decodes PNG and JPEG only. ImageSharp's default Configuration sniffs the real
/// format from file contents, so a TIFF renamed to .jpg would be decoded as TIFF.
/// This loader uses a Configuration with only the PNG and JPEG modules registered,
/// so TIFF/BMP/GIF/WEBP/QOI/TGA/PBM content cannot reach a decoder at all.
/// </summary>
public static class SafeImageLoader
{
    public static Configuration Configuration { get; } = CreateConfiguration();

    private static Configuration CreateConfiguration()
    {
        var configuration = new Configuration(new PngConfigurationModule(), new JpegConfigurationModule())
        {
            MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
            {
                AllocationLimitMegabytes = DecodeLimits.AllocationLimitMegabytes
            })
        };
        return configuration;
    }

    public static Image<Rgba32> Load(string path, DecodeLimits? limits = null)
    {
        limits ??= DecodeLimits.Default;
        var options = new DecoderOptions { Configuration = Configuration, MaxFrames = 1 };

        var length = new FileInfo(path).Length;
        if (length > limits.MaxFileBytes)
        {
            throw new ImageRejectedException(
                $"rejected: file is {length:N0} bytes, over the {limits.MaxFileBytes:N0}-byte limit");
        }

        ImageInfo info;
        try
        {
            info = Image.Identify(options, path);
        }
        catch (UnknownImageFormatException)
        {
            throw new ImageRejectedException($"rejected: content is {DescribeRealFormat(path)}, not PNG or JPEG");
        }

        var format = info.Metadata.DecodedImageFormat;
        if (format is not (PngFormat or JpegFormat))
        {
            throw new ImageRejectedException($"rejected: content is {format?.Name ?? "unknown"}, not PNG or JPEG");
        }

        if (info.Width <= 0 || info.Height <= 0)
        {
            throw new ImageRejectedException("rejected: image header has no usable dimensions");
        }

        var pixels = (long)info.Width * info.Height;
        if (info.Width > limits.MaxWidth || info.Height > limits.MaxHeight || pixels > limits.MaxPixels)
        {
            throw new ImageRejectedException(
                $"rejected: {info.Width}x{info.Height} ({pixels:N0} px) exceeds the limit of " +
                $"{limits.MaxWidth}x{limits.MaxHeight} / {limits.MaxPixels:N0} px");
        }

        return Image.Load<Rgba32>(options, path);
    }

    /// <summary>
    /// Header-only sniff with the default format list, used only to name the real
    /// format in the rejection message. Detection reads magic bytes; it never decodes.
    /// </summary>
    private static string DescribeRealFormat(string path)
    {
        try
        {
            return Image.DetectFormat(path).Name;
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or IOException or NotSupportedException)
        {
            return "an unrecognized format";
        }
    }
}
