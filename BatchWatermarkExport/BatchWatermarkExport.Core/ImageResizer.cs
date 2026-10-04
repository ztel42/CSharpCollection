using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BatchWatermarkExport;

/// <summary>
/// Center cover-crop. Scales uniformly until the frame is filled, then crops
/// the overflow from the center. Never stretches and never letterboxes.
/// </summary>
public static class ImageResizer
{
    public static void CoverCrop(Image image, int targetWidth, int targetHeight)
    {
        if (targetWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWidth));
        }

        if (targetHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetHeight));
        }

        if (image.Width == targetWidth && image.Height == targetHeight)
        {
            return;
        }

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetHeight),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center,
            Sampler = KnownResamplers.Lanczos3,
            Compand = false
        }));
    }
}
