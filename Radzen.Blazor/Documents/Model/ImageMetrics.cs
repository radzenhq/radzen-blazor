using System;
using Radzen.Documents.Core;

namespace Radzen.Documents;

internal enum ImageFormat
{
    Png,
    Jpeg,
    Jpeg2000,
}

internal sealed record ImageInfo(ImageFormat Format, double Width, double Height);

internal static class ImageMetrics
{
    public static string? MediaType(ImageFormat format) => format switch
    {
        ImageFormat.Png => "image/png",
        ImageFormat.Jpeg => "image/jpeg",
        ImageFormat.Jpeg2000 => "image/jp2",
        _ => null,
    };

    public static (double Width, double Height) Measure(Image image, double pixelWidth, double pixelHeight, double availableWidth)
    {
        var (baseWidth, baseHeight) = DeriveSize(image.Width, image.Height, pixelWidth, pixelHeight);

        if (image.FitBox is { } box)
        {
            var scale = Math.Min(box.MaxWidth.Point / baseWidth, box.MaxHeight.Point / baseHeight);
            return (baseWidth * scale, baseHeight * scale);
        }

        if (image.Width is null && image.Height is null
            && availableWidth > 0 && !double.IsInfinity(availableWidth) && baseWidth > availableWidth)
        {
            baseHeight *= availableWidth / baseWidth;
            baseWidth = availableWidth;
        }

        return (baseWidth, baseHeight);
    }

    public static (double Width, double Height) DeriveSize(Unit? width, Unit? height, double pixelWidth, double pixelHeight)
    {
        if (width is { } w && height is { } h)
        {
            return (w.Point, h.Point);
        }

        if (width is { } wo)
        {
            return (wo.Point, pixelHeight * wo.Point / pixelWidth);
        }

        if (height is { } ho)
        {
            return (pixelWidth * ho.Point / pixelHeight, ho.Point);
        }

        return (pixelWidth * 72.0 / 96.0, pixelHeight * 72.0 / 96.0);
    }
}
