using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf;

internal sealed class WatermarkContent(Watermark watermark, PdfRect box, ImageStore images) : ContentElement
{
    protected override void EmitBody(ContentWriter writer)
    {
        writer.WriteRaw("q\n");
        if (watermark.Opacity < 1)
        {
            writer.WriteName(writer.RegisterOpacity(watermark.Opacity));
            writer.WriteRaw(" gs\n");
        }

        ContentEmitter.WriteTransform(
            writer,
            WatermarkGeometry.Rotation(
                watermark.Rotation,
                box.Left + box.Width / 2,
                box.Bottom + box.Height / 2));

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

        var decoded = images.DecodeWatermark(image).Image;
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

        var fontKey = writer.RegisterFont(watermark.Font);
        var plan = WatermarkTextPlanning.Plan(watermark.Text, watermark);
        ContentEmitter.WriteTextShow(writer, new TextShowOp
        {
            FontKey = fontKey,
            Size = watermark.Font.EffectiveSize.Point,
            X = plan.X,
            Baseline = plan.Baseline,
            Color = watermark.Font.EffectiveColor,
            Bytes = plan.Base14Bytes!,
            ExtGState = plan.AlphaOverride is { } alpha ? writer.RegisterOpacity(alpha) : null,
        });
    }
}

internal readonly struct WatermarkTextLayout
{
    public required byte[]? Base14Bytes { get; init; }

    public required double X { get; init; }

    public required double Baseline { get; init; }

    public required double? AlphaOverride { get; init; }

    public bool IsSfnt => Base14Bytes is null;
}

internal static class WatermarkTextPlanning
{
    public static WatermarkTextLayout Plan(
        string text, Watermark watermark, FontCollection? fonts = null)
    {
        var font = watermark.Font;
        var alphaOverride = WatermarkGeometry.AlphaOverride(watermark.Opacity, font.EffectiveColor.A);

        if (fonts is not null && fonts.TryResolvePrimary(font, out _))
        {
            return new WatermarkTextLayout
            {
                Base14Bytes = null,
                X = WatermarkGeometry.Centered(fonts.MeasureText(text, font)),
                Baseline = -WatermarkGeometry.Baseline(font.EffectiveSize.Point),
                AlphaOverride = alphaOverride,
            };
        }

        var base14 = WatermarkTextPlan.Base14(text, font);
        return new WatermarkTextLayout
        {
            Base14Bytes = base14.Bytes,
            X = base14.X,
            Baseline = base14.Baseline,
            AlphaOverride = alphaOverride,
        };
    }
}
