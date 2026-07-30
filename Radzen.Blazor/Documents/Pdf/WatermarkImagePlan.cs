using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Pdf;

internal readonly record struct WatermarkImagePlan(
    double X,
    double Y,
    double Width,
    double Height,
    double Alpha)
{
    public static WatermarkImagePlan Create(Image image, DecodedImage decoded, double availableWidth)
    {
        var (width, height) = ImageDecoder.Measure(image, decoded, availableWidth);
        return new(
            WatermarkGeometry.Centered(width),
            WatermarkGeometry.Centered(height),
            width,
            height,
            image.Opacity);
    }
}
