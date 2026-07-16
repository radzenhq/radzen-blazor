using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf;

internal sealed class WatermarkContent(Watermark watermark, Rect box) : ContentElement
{
    protected override void EmitBody(ContentWriter writer)
    {
        writer.WriteRaw("q\n");
        if (watermark.Opacity < 1)
        {
            writer.WriteName(writer.RegisterOpacity(watermark.Opacity));
            writer.WriteRaw(" gs\n");
        }

        var radians = watermark.Rotation * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(sin);
        writer.WriteRaw(" ");
        writer.WriteNumber(sin == 0 ? 0 : -sin);
        writer.WriteRaw(" ");
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(box.X + box.Width / 2);
        writer.WriteRaw(" ");
        writer.WriteNumber(box.Y + box.Height / 2);
        writer.WriteRaw(" cm\n");

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
        writer.WriteNumber(-width / 2);
        writer.WriteRaw(" ");
        writer.WriteNumber(-height / 2);
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

        var bytes = Encode(watermark.Text);
        var metrics = Base14Metrics.Resolve(watermark.Font) ?? Base14Metrics.Resolve(new Font())!;
        var x = -metrics.MeasureString(watermark.Text, watermark.Font.Size) / 2;
        var fontKey = writer.RegisterFont(watermark.Font);
        writer.WriteRaw("BT\n");
        writer.WriteColor(watermark.Font.Color, "rg");
        writer.WriteName(fontKey);
        writer.WriteRaw(" ");
        writer.WriteNumber(watermark.Font.Size);
        writer.WriteRaw(" Tf\n");
        writer.WriteNumber(x);
        writer.WriteRaw(" ");
        writer.WriteNumber(-watermark.Font.Size * 0.35);
        writer.WriteRaw(" Td\n");
        writer.WriteString(bytes);
        writer.WriteRaw(" Tj\nET\n");
    }

    private static byte[] Encode(string text)
    {
        var bytes = new List<byte>(text.Length);
        foreach (var character in text)
        {
            if (!WinAnsiEncoding.TryGetCode(character, out var code))
            {
                throw new NotSupportedException(
                    $"Watermark text contains a character (U+{(int)character:X4}) not representable in the base-14 WinAnsi encoding.");
            }

            bytes.Add(code);
        }

        return [.. bytes];
    }
}
