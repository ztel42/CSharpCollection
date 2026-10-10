using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using BatchWatermarkExport.Cli;

namespace BatchWatermarkExport.Tests;

public class PipelineTests
{
    private static readonly Rgba32 Background = new(10, 20, 180);

    [Fact]
    public void Watermark_is_visible_in_the_chosen_corner()
    {
        using var dir = new TempTree();
        var source = dir.InputFile("still.png");
        WriteSolid(source, 480, 320, Background);

        var result = Export(dir, "WM", WatermarkPosition.TopLeft, 1f, 48f, ExifPolicy.Strip, PlatformPreset.FullResPortfolio);

        Assert.Single(result.Written);
        using var image = Image.Load<Rgba32>(result.Written[0].OutputPath);
        Assert.Equal(480, image.Width);
        Assert.Equal(320, image.Height);
        Assert.True(CountDiff(image, Background, 0, 0, 240, 160) > 30, "expected watermark pixels in the top-left");
        Assert.Equal(0, CountDiff(image, Background, 240, 160, 480, 320));
    }

    [Fact]
    public void Watermark_bottom_right_does_not_touch_the_opposite_corner()
    {
        using var dir = new TempTree();
        var source = dir.InputFile("still.png");
        WriteSolid(source, 480, 320, Background);

        var result = Export(dir, "WM", WatermarkPosition.BottomRight, 1f, 48f, ExifPolicy.Strip, PlatformPreset.FullResPortfolio);
        using var image = Image.Load<Rgba32>(result.Written[0].OutputPath);
        Assert.True(CountDiff(image, Background, 240, 160, 480, 320) > 30);
        Assert.Equal(0, CountDiff(image, Background, 0, 0, 240, 160));
    }

    [Fact]
    public void Opacity_blends_instead_of_painting_solid_white()
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("still.png"), 480, 320, Background);
        var result = Export(dir, "WM", WatermarkPosition.TopLeft, 0.4f, 64f, ExifPolicy.Strip, PlatformPreset.FullResPortfolio);
        using var image = Image.Load<Rgba32>(result.Written[0].OutputPath);

        var strongest = 0;
        var changed = 0;
        for (var y = 0; y < 160; y++)
        {
            for (var x = 0; x < 240; x++)
            {
                var pixel = image[x, y];
                if (pixel == Background)
                {
                    continue;
                }

                changed++;
                strongest = Math.Max(strongest, pixel.R);
            }
        }

        Assert.True(changed > 30);
        Assert.InRange(strongest, Background.R + 20, 250);
    }

    [Theory]
    [InlineData(PlatformPreset.YouTubeThumbnail, 1280, 720)]
    [InlineData(PlatformPreset.InstagramSquare, 1080, 1080)]
    [InlineData(PlatformPreset.InstagramPortrait, 1080, 1350)]
    public void Preset_exports_exact_canvas(PlatformPreset preset, int width, int height)
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("wide.png"), 400, 200, Background);
        var result = Export(dir, "YT", WatermarkPosition.Center, 0.8f, 28f, ExifPolicy.Strip, preset);
        using var image = Image.Load<Rgba32>(result.Written[0].OutputPath);
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
    }

    [Fact]
    public void Full_res_portfolio_keeps_original_pixel_size()
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("master.png"), 400, 200, Background);
        var result = Export(dir, "", WatermarkPosition.Center, 1f, 0f, ExifPolicy.Strip, PlatformPreset.FullResPortfolio);
        using var image = Image.Load<Rgba32>(result.Written[0].OutputPath);
        Assert.Equal(400, image.Width);
        Assert.Equal(200, image.Height);
        Assert.Equal(0, CountDiff(image, Background, 0, 0, 400, 200));
    }

    [Fact]
    public void Cover_crop_fills_the_frame_without_stretching()
    {
        using var image = new Image<Rgba32>(400, 200);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = x < 80 || x >= 320
                        ? new Rgba32(0, 180, 0)
                        : new Rgba32(220, 0, 0);
                }
            }
        });

        ImageResizer.CoverCrop(image, 200, 200);

        Assert.Equal(200, image.Width);
        Assert.Equal(200, image.Height);
        var left = image[0, 100];
        var right = image[199, 100];
        Assert.True(left.R > 200 && left.G < 20, $"left edge was stretched or left-cropped: {left}");
        Assert.True(right.R > 200 && right.G < 20, $"right edge was stretched or left-cropped: {right}");
    }

    [Fact]
    public void Exif_keep_copies_make_and_normalizes_orientation()
    {
        using var dir = new TempTree();
        var source = dir.InputFile("tagged.png");
        using (var image = new Image<Rgba32>(40, 10, new Rgba32(255, 255, 255)))
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.Make, "BatchWatermarkExportTest");
            profile.SetValue(ExifTag.Orientation, (ushort)6);
            image.Metadata.ExifProfile = profile;
            image.SaveAsPng(source);
        }

        var result = Export(dir, "EXIF", WatermarkPosition.Center, 1f, 12f, ExifPolicy.Keep, PlatformPreset.FullResPortfolio);
        using var saved = Image.Load<Rgba32>(result.Written[0].OutputPath);
        Assert.Equal(10, saved.Width);
        Assert.Equal(40, saved.Height);
        Assert.NotNull(saved.Metadata.ExifProfile);
        Assert.True(saved.Metadata.ExifProfile!.TryGetValue(ExifTag.Make, out IExifValue<string>? make));
        Assert.Equal("BatchWatermarkExportTest", make!.Value);
        Assert.True(saved.Metadata.ExifProfile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? orientation));
        Assert.Equal((ushort)1, orientation!.Value);
    }

    [Fact]
    public void Exif_strip_removes_the_profile()
    {
        using var dir = new TempTree();
        var source = dir.InputFile("tagged.jpg");
        using (var image = new Image<Rgba32>(32, 24, new Rgba32(30, 30, 30)))
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.Make, "ShouldBeStripped");
            profile.SetValue(ExifTag.Model, "TestBody");
            image.Metadata.ExifProfile = profile;
            image.SaveAsJpeg(source);
        }

        var before = File.ReadAllBytes(source);
        var result = Export(dir, "X", WatermarkPosition.BottomLeft, 1f, 16f, ExifPolicy.Strip, PlatformPreset.FullResPortfolio);
        Assert.Equal(before, File.ReadAllBytes(source));

        using var saved = Image.Load<Rgba32>(result.Written[0].OutputPath);
        Assert.Null(saved.Metadata.ExifProfile);
        Assert.Null(saved.Metadata.XmpProfile);
        Assert.Null(saved.Metadata.IptcProfile);
    }

    [Fact]
    public void Keep_survives_a_platform_resize()
    {
        using var dir = new TempTree();
        var source = dir.InputFile("small.png");
        using (var image = new Image<Rgba32>(64, 32, Background))
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.ImageDescription, "portfolio-keep");
            profile.SetValue(ExifTag.PixelXDimension, new Number(64u));
            profile.SetValue(ExifTag.PixelYDimension, new Number(32u));
            image.Metadata.ExifProfile = profile;
            image.SaveAsPng(source);
        }

        var result = Export(dir, "IG", WatermarkPosition.BottomRight, 0.5f, 20f, ExifPolicy.Keep, PlatformPreset.InstagramSquare);
        using var saved = Image.Load<Rgba32>(result.Written[0].OutputPath);
        Assert.Equal(1080, saved.Width);
        Assert.Equal(1080, saved.Height);
        Assert.NotNull(saved.Metadata.ExifProfile);
        Assert.True(saved.Metadata.ExifProfile!.TryGetValue(ExifTag.ImageDescription, out IExifValue<string>? description));
        Assert.Equal("portfolio-keep", description!.Value);
        Assert.True(saved.Metadata.ExifProfile.TryGetValue(ExifTag.PixelXDimension, out IExifValue<Number>? px));
        Assert.Equal(1080u, (uint)px!.Value);
        Assert.True(saved.Metadata.ExifProfile.TryGetValue(ExifTag.PixelYDimension, out IExifValue<Number>? py));
        Assert.Equal(1080u, (uint)py!.Value);
    }

    [Fact]
    public void Skips_non_images_and_does_not_modify_sources()
    {
        using var dir = new TempTree();
        var png = dir.InputFile("photo.PNG");
        var jpg = dir.InputFile("photo.jpg");
        var notes = dir.InputFile("notes.txt");
        var nestedDir = Path.Combine(dir.Input, "nested");
        Directory.CreateDirectory(nestedDir);
        var nested = Path.Combine(nestedDir, "hidden.png");
        WriteSolid(png, 20, 20, Background);
        WriteSolid(jpg, 30, 16, new Rgba32(1, 2, 3));
        File.WriteAllText(notes, "not an image");
        WriteSolid(nested, 12, 12, Background);
        var pngBefore = File.ReadAllBytes(png);
        var jpgBefore = File.ReadAllBytes(jpg);
        var notesBefore = File.ReadAllBytes(notes);

        var result = BatchExporter.Export(new ExportRequest(
            dir.Input,
            dir.Output,
            new WatermarkOptions("OK", WatermarkPosition.TopLeft, 1f, 12f),
            ExifPolicy.Strip,
            [PlatformPreset.FullResPortfolio]));

        Assert.Equal(2, result.Written.Count);
        Assert.Contains(result.Skipped, path => path.EndsWith("notes.txt", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Written, item => item.SourcePath.EndsWith("hidden.png", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Skipped, path => path.EndsWith("hidden.png", StringComparison.Ordinal));
        Assert.Equal(pngBefore, File.ReadAllBytes(png));
        Assert.Equal(jpgBefore, File.ReadAllBytes(jpg));
        Assert.Equal(notesBefore, File.ReadAllBytes(notes));
        Assert.All(result.Written, item => Assert.StartsWith(dir.Output, item.OutputPath));
        using var exportedPng = Image.Load<Rgba32>(result.Written.Single(item => item.OutputPath.EndsWith(".PNG", StringComparison.Ordinal)).OutputPath);
        Assert.Equal(20, exportedPng.Width);
        Assert.Equal(20, exportedPng.Height);
    }

    [Fact]
    public void Refuses_to_write_inside_the_input_folder()
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("a.png"), 8, 8, Background);
        var inside = Path.Combine(dir.Input, "out");
        var ex = Assert.Throws<ArgumentException>(() => ExportTo(dir.Input, inside));
        Assert.Contains("outside", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cli_exports_a_folder()
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("shot.png"), 80, 40, Background);
        File.WriteAllText(dir.InputFile("readme.md"), "skip me");

        var stdout = Console.Out;
        var stderr = Console.Error;
        using var sink = new StringWriter();
        Console.SetOut(sink);
        Console.SetError(sink);
        int exit;
        try
        {
            exit = Program.Main(
            [
                "--input", dir.Input,
                "--output", dir.Output,
                "--text", "CLI",
                "--position", "center",
                "--opacity", "0.9",
                "--font-size", "18",
                "--preset", "youtube,full-res",
                "--exif", "strip"
            ]);
        }
        finally
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
        }

        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(dir.Output, "shot__youtube-1280x720.png")));
        Assert.True(File.Exists(Path.Combine(dir.Output, "shot__full-res.png")));
        using var thumb = Image.Load<Rgba32>(Path.Combine(dir.Output, "shot__youtube-1280x720.png"));
        Assert.Equal(1280, thumb.Width);
        Assert.Equal(720, thumb.Height);
        Assert.Null(thumb.Metadata.ExifProfile);
    }

    [Fact]
    public void Tiff_renamed_to_jpg_is_rejected_and_the_batch_continues()
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("good.png"), 40, 30, Background);
        WriteSolid(dir.InputFile("good.jpg"), 40, 30, Background);
        using (var tiff = new Image<Rgba32>(40, 30, Background))
        {
            tiff.SaveAsTiff(dir.InputFile("evil.jpg"));
        }

        using (var bmp = new Image<Rgba32>(40, 30, Background))
        {
            bmp.SaveAsBmp(dir.InputFile("sneaky.png"));
        }

        var result = Export(dir, "WM", WatermarkPosition.TopLeft, 1f, 12f, ExifPolicy.Strip, PlatformPreset.FullResPortfolio);

        Assert.Equal(2, result.Written.Count);
        Assert.Contains(result.Written, item => item.SourcePath.EndsWith("good.png", StringComparison.Ordinal));
        Assert.Contains(result.Written, item => item.SourcePath.EndsWith("good.jpg", StringComparison.Ordinal));
        Assert.Equal(2, result.Failed.Count);
        Assert.Contains(result.Failed, f => f.Contains("evil.jpg", StringComparison.Ordinal) && f.Contains("TIFF", StringComparison.OrdinalIgnoreCase) && f.Contains("rejected", StringComparison.Ordinal));
        Assert.Contains(result.Failed, f => f.Contains("sneaky.png", StringComparison.Ordinal) && f.Contains("BMP", StringComparison.OrdinalIgnoreCase));
        Assert.False(File.Exists(Path.Combine(dir.Output, "evil__full-res.jpg")));
    }

    [Fact]
    public void Safe_loader_cannot_decode_tiff_even_when_called_directly()
    {
        using var dir = new TempTree();
        var path = dir.InputFile("photo.jpeg");
        using (var tiff = new Image<Rgba32>(8, 8, Background))
        {
            tiff.SaveAsTiff(path);
        }

        Assert.Throws<ImageRejectedException>(() => SafeImageLoader.Load(path));
        Assert.Throws<UnknownImageFormatException>(() => Image.Load<Rgba32>(new SixLabors.ImageSharp.Formats.DecoderOptions { Configuration = SafeImageLoader.Configuration }, path));
    }

    [Fact]
    public void Valid_png_and_jpeg_are_still_watermarked_through_the_safe_loader()
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("a.png"), 480, 320, Background);
        WriteSolid(dir.InputFile("b.jpg"), 480, 320, Background);

        var result = Export(dir, "WM", WatermarkPosition.TopLeft, 1f, 48f, ExifPolicy.Strip, PlatformPreset.FullResPortfolio);

        Assert.Empty(result.Failed);
        Assert.Equal(2, result.Written.Count);
        using var png = Image.Load<Rgba32>(result.Written.Single(w => w.OutputPath.EndsWith(".png", StringComparison.Ordinal)).OutputPath);
        Assert.True(CountDiff(png, Background, 0, 0, 240, 160) > 30);
        using var jpg = Image.Load<Rgba32>(result.Written.Single(w => w.OutputPath.EndsWith(".jpg", StringComparison.Ordinal)).OutputPath);
        Assert.Equal(480, jpg.Width);
        Assert.Equal(320, jpg.Height);
    }

    [Fact]
    public void Oversized_image_is_rejected_before_decode_and_the_batch_continues()
    {
        using var dir = new TempTree();
        WriteSolid(dir.InputFile("big.png"), 300, 200, Background);
        WriteSolid(dir.InputFile("small.png"), 50, 40, Background);
        var limits = new DecodeLimits(DecodeLimits.DefaultMaxFileBytes, 100, 100, 10_000);

        var result = BatchExporter.Export(new ExportRequest(
            dir.Input,
            dir.Output,
            new WatermarkOptions("WM", WatermarkPosition.Center, 1f, 12f),
            ExifPolicy.Strip,
            [PlatformPreset.FullResPortfolio],
            limits));

        Assert.Single(result.Written);
        Assert.EndsWith("small.png", result.Written[0].SourcePath, StringComparison.Ordinal);
        var failure = Assert.Single(result.Failed);
        Assert.Contains("big.png", failure, StringComparison.Ordinal);
        Assert.Contains("300x200", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void File_over_the_size_limit_is_rejected()
    {
        using var dir = new TempTree();
        var path = dir.InputFile("heavy.png");
        WriteSolid(path, 64, 64, Background);
        var limits = DecodeLimits.Default with { MaxFileBytes = 16 };

        var ex = Assert.Throws<ImageRejectedException>(() => SafeImageLoader.Load(path, limits));
        Assert.Contains("byte limit", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Default_limits_are_documented_values()
    {
        Assert.Equal(100L * 1024 * 1024, DecodeLimits.Default.MaxFileBytes);
        Assert.Equal(20_000, DecodeLimits.Default.MaxWidth);
        Assert.Equal(20_000, DecodeLimits.Default.MaxHeight);
        Assert.Equal(100_000_000, DecodeLimits.Default.MaxPixels);
    }

    private static ExportResult Export(
        TempTree dir,
        string text,
        WatermarkPosition position,
        float opacity,
        float fontSize,
        ExifPolicy exif,
        PlatformPreset preset)
    {
        return BatchExporter.Export(new ExportRequest(
            dir.Input,
            dir.Output,
            new WatermarkOptions(text, position, opacity, fontSize),
            exif,
            [preset]));
    }

    private static void ExportTo(string input, string output)
    {
        BatchExporter.Export(new ExportRequest(
            input,
            output,
            new WatermarkOptions("nope", WatermarkPosition.Center, 1f, 12f),
            ExifPolicy.Strip,
            [PlatformPreset.FullResPortfolio]));
    }

    private static void WriteSolid(string path, int width, int height, Rgba32 color)
    {
        using var image = new Image<Rgba32>(width, height, color);
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            image.SaveAsJpeg(path);
        }
        else
        {
            image.SaveAsPng(path);
        }
    }

    private static int CountDiff(Image<Rgba32> image, Rgba32 background, int x0, int y0, int x1, int y1)
    {
        var count = 0;
        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
            {
                if (image[x, y] != background)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private sealed class TempTree : IDisposable
    {
        public TempTree()
        {
            Root = Path.Combine(Path.GetTempPath(), "bwe-" + Guid.NewGuid().ToString("N"));
            Input = Path.Combine(Root, "in");
            Output = Path.Combine(Root, "out");
            Directory.CreateDirectory(Input);
        }

        public string Root { get; }
        public string Input { get; }
        public string Output { get; }

        public string InputFile(string name) => Path.Combine(Input, name);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
