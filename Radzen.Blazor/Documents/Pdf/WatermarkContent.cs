using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Fonts;

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

        var bytes = WinAnsiText.Encode(watermark.Text, OnUnencodable.Throw, WatermarkGeometry.EncodingContext);
        var metrics = Base14Metrics.Resolve(watermark.Font) ?? Base14Metrics.Resolve(new Font())!;
        ContentEmitter.WriteTextShow(writer, new TextShowOp
        {
            FontKey = writer.RegisterFont(watermark.Font),
            Size = watermark.Font.Size,
            X = WatermarkGeometry.Centered(metrics.MeasureString(watermark.Text, watermark.Font.Size)),
            Baseline = WatermarkGeometry.Baseline(watermark.Font.Size),
            Color = watermark.Font.Color,
            Bytes = bytes,
        });
    }
}
