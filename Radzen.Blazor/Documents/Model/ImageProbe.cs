using System;

namespace Radzen.Documents;

internal enum ImageFormat
{
    Png,
    Jpeg,
    Jpeg2000,
    Custom,
}

internal sealed record ImageInfo(ImageFormat Format, double Width, double Height);

internal static class ImageProbe
{
    private const int SizeProbeLimit = 64;

    private static readonly object SizeProbeGate = new();

    private static volatile ImageProbes registered = ImageProbes.None;

    public static ImageProbes Registered => registered;

    public static void RegisterSizeProbe(Func<ReadOnlyMemory<byte>, (double Width, double Height)?> probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        lock (SizeProbeGate)
        {
            var snapshot = registered;
            if (snapshot.Contains(probe))
            {
                return;
            }

            if (snapshot.Count >= SizeProbeLimit)
            {
                throw new InvalidOperationException($"No more than {SizeProbeLimit} custom image size probes can be registered.");
            }

            registered = snapshot.Add(probe);
        }
    }

    public static ImageInfo Inspect(byte[] data) => registered.Inspect(data);

    public static ImageFormat Format(byte[] data) => registered.Format(data);

    public static (double Width, double Height) PixelSize(byte[] data) => registered.PixelSize(data);

    public static string? MediaType(ImageFormat format) => format switch
    {
        ImageFormat.Png => "image/png",
        ImageFormat.Jpeg => "image/jpeg",
        ImageFormat.Jpeg2000 => "image/jp2",
        _ => null,
    };

    public static (double Width, double Height) Measure(Image image, double availableWidth)
        => registered.Measure(image, availableWidth);

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
