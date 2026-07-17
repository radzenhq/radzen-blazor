using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;

namespace Radzen.Documents.Pdf;

internal sealed class WatermarkContent(Watermark watermark, PdfRect box) : ContentElement
{
    protected override void EmitBody(ContentWriter writer)
    {
        writer.WriteRaw("q\n");
        if (watermark.Opacity < 1)
        {
            writer.WriteName(writer.RegisterOpacity(watermark.Opacity));
            writer.WriteRaw(" gs\n");
        }

        WatermarkGeometry.WriteRotation(
            writer, watermark.Rotation, box.Left + box.Width / 2, box.Bottom + box.Height / 2);

        WriteImage(writer);
        WriteText(writer);
        writer.WriteRaw("Q\n");
    }

    private void WriteImage(ContentWriter writer)
    {
        if (watermark.Image is not { } image)
        {
            return;
        }

        var decoded = watermark.DecodeImage(image);
        var (width, height) = ImageDecoder.Measure(image, decoded, box.Width);
        var key = writer.RegisterImage(decoded);
        ContentEmitter.WriteImagePlacement(
            writer, key, WatermarkGeometry.Centered(width), WatermarkGeometry.Centered(height), width, height);
    }

    private void WriteText(ContentWriter writer)
    {
        if (string.IsNullOrEmpty(watermark.Text))
        {
            return;
        }

        var plan = WatermarkTextPlan.Base14(watermark.Text, watermark.Font);
        ContentEmitter.WriteTextShow(writer, new TextShowOp
        {
            FontKey = writer.RegisterFont(watermark.Font),
            Size = watermark.Font.Size,
            X = plan.X,
            Baseline = plan.Baseline,
            Color = watermark.Font.Color,
            Bytes = plan.Bytes,
        });
    }
}
