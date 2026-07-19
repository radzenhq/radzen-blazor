using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;

namespace Radzen.Documents.Pdf;

internal readonly record struct WatermarkImagePlan(
    double X,
    double Y,
    double Width,
    double Height,
    double Alpha,
    Color? StencilColor)
{
    public static WatermarkImagePlan Create(Image image, ImageXObject decoded, double availableWidth)
    {
        var (width, height) = ImageDecoder.Measure(image, decoded, availableWidth);
        var alpha = image.Opacity;
        if (image.Stencil && image.StencilColor.A != 255)
        {
            alpha *= image.StencilColor.A / 255.0;
        }

        return new(
            WatermarkGeometry.Centered(width),
            WatermarkGeometry.Centered(height),
            width,
            height,
            alpha,
            image.Stencil ? image.StencilColor : null);
    }
}
