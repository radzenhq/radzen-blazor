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
        writer.WriteRaw("q\n");
        writer.WriteNumber(width);
        writer.WriteRaw(" 0 0 ");
        writer.WriteNumber(height);
        writer.WriteRaw(" ");
        writer.WriteNumber(WatermarkGeometry.Centered(width));
        writer.WriteRaw(" ");
        writer.WriteNumber(WatermarkGeometry.Centered(height));
        writer.WriteRaw(" cm\n");
        writer.WriteName(key);
        writer.WriteRaw(" Do\nQ\n");
    }

    private void WriteText(ContentWriter writer)
    {
        if (string.IsNullOrEmpty(watermark.Text))
        {
            return;
        }

        // This stream cannot embed a font file (Document.FontScope, CanEmbed: false), so a
        // watermark in a registered family is rejected here by RegisterFont rather than
        // embedded as the authored path embeds it.
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
