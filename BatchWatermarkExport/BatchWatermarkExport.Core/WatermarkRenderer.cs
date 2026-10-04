using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BatchWatermarkExport;

public static class WatermarkRenderer
{
    public const float MinAutoFontSize = 18f;
    public const float MaxAutoFontSize = 96f;

    public static float ResolveFontSize(int imageHeight, float requested)
    {
        if (requested > 0)
        {
            return requested;
        }

        var auto = imageHeight / 16f;
        return Math.Clamp(auto, MinAutoFontSize, MaxAutoFontSize);
    }

    public static void Apply(Image<Rgba32> image, WatermarkOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Text))
        {
            return;
        }

        var opacity = Math.Clamp(options.Opacity, 0f, 1f);
        if (opacity <= 0f)
        {
            return;
        }

        var fontSize = ResolveFontSize(image.Height, options.FontSize);
        var font = FontAssets.Create(fontSize);
        var measured = TextMeasurer.MeasureAdvance(options.Text, new TextOptions(font));
        var textWidth = measured.Width;
        var textHeight = measured.Height > 1f ? measured.Height : fontSize;
        var margin = Math.Max(12f, Math.Min(image.Width, image.Height) * 0.02f);

        float x = options.Position switch
        {
            WatermarkPosition.TopRight or WatermarkPosition.BottomRight => image.Width - textWidth - margin,
            WatermarkPosition.Center => (image.Width - textWidth) / 2f,
            _ => margin
        };

        float y = options.Position switch
        {
            WatermarkPosition.BottomLeft or WatermarkPosition.BottomRight => image.Height - textHeight - margin,
            WatermarkPosition.Center => (image.Height - textHeight) / 2f,
            _ => margin
        };

        var alpha = (byte)Math.Round(opacity * 255f);
        var color = Color.FromRgba(255, 255, 255, alpha);
        var drawOptions = new RichTextOptions(font)
        {
            Origin = new PointF(x, y),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        image.Mutate(ctx => ctx.DrawText(drawOptions, options.Text, color));
    }
}
