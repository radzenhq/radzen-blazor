using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Render;

namespace Radzen.Documents.Pdf;

internal sealed class WatermarkContent(
    Watermark watermark,
    PdfRect box,
    ImageStore images,
    SourceId? imageSource,
    SceneImageData? imageData) : ContentElement
{
    protected override void EmitBody(ContentWriter writer)
    {
        var extGState = watermark.Opacity < 1
            ? writer.RegisterOpacity(watermark.Opacity)
            : null;
        var transform = WatermarkGeometry.Rotation(
            watermark.Rotation,
            box.Left + box.Width / 2,
            box.Bottom + box.Height / 2);
        ContentEmitter.WriteWatermark(
            writer,
            extGState,
            transform,
            WriteImage,
            WriteText);
    }

    private void WriteImage(ContentWriter writer)
    {
        if (watermark.Image is not { } image
            || imageSource is not { } source
            || imageData is not { } data)
        {
            return;
        }

        var paint = new ImagePaint
        {
            Data = data,
            Opacity = image.Opacity,
            Interpolate = image.Interpolate,
        };
        var decoded = ImageStore.SourceOf(images.DecodeWatermark(source, paint));
        var plan = WatermarkImagePlan.Create(image, decoded, box.Width);
        var key = writer.RegisterImage(decoded);
        var state = plan.Alpha < 1
            ? writer.RegisterOpacity(watermark.Opacity * plan.Alpha)
            : null;
        ContentEmitter.WriteImagePlacement(
            writer, key, plan.X, plan.Y, plan.Width, plan.Height, state);
    }

    private void WriteText(ContentWriter writer)
    {
        if (string.IsNullOrEmpty(watermark.Text))
        {
            return;
        }

        var font = watermark.Font;
        var fontKey = writer.RegisterFont(font);
        var plan = WatermarkTextPlan.Base14(watermark.Text, font);
        var alphaOverride = WatermarkGeometry.AlphaOverride(watermark.Opacity, font.EffectiveColor.A);
        ContentEmitter.WriteTextShow(writer, new TextShowOp
        {
            FontKey = fontKey,
            Size = font.EffectiveSize.Point,
            X = plan.X,
            Baseline = plan.Baseline,
            Color = font.EffectiveColor,
            Bytes = plan.Bytes,
            ExtGState = alphaOverride is { } alpha ? writer.RegisterOpacity(alpha) : null,
        });
    }
}
