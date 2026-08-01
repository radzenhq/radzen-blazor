using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

internal sealed class WatermarkContent(
    Watermark watermark,
    PdfRect box,
    ImageRegistry images,
    FontCollectionSnapshot? knownFonts,
    ImageProbes probes) : ContentElement
{
    protected override void EmitBody(ContentWriter writer)
    {
        var planned = WatermarkPlanner.PlanBase14(
            watermark,
            new PageSize(Unit.FromPoint(box.Width), Unit.FromPoint(box.Height)),
            knownFonts,
            new LayoutCaptureContext(probes))!;
        var extGState = planned.Opacity < 1
            ? writer.RegisterOpacity(planned.Opacity)
            : null;
        var transform = WatermarkGeometry.Rotation(
            planned.Rotation,
            box.Left + planned.CenterX,
            box.Bottom + planned.CenterY);
        ContentEmitter.WriteWatermark(
            writer,
            extGState,
            transform,
            output => WriteImage(output, planned),
            output => WriteText(output, planned));
    }

    private void WriteImage(ContentWriter writer, LaidOutWatermark planned)
    {
        if (planned.Image is not { } image)
        {
            return;
        }

        var decoded = ImageRegistry.SourceOf(images.DecodeWatermark(image.Source, image.Paint));
        var key = writer.RegisterImage(decoded);
        var state = image.Alpha < 1
            ? writer.RegisterOpacity(planned.Opacity * image.Alpha)
            : null;
        ContentEmitter.WriteImagePlacement(
            writer, key, image.X, image.Y, image.Width, image.Height, state);
    }

    private void WriteText(ContentWriter writer, LaidOutWatermark planned)
    {
        if (planned.Text is not { } text)
        {
            return;
        }

        var font = watermark.Font;
        var fontKey = writer.RegisterFont(font);
        ContentEmitter.WriteTextShow(writer, new TextShowOp
        {
            FontKey = fontKey,
            Size = text.Size,
            X = text.X,
            Baseline = -text.Baseline,
            Color = text.Color,
            Bytes = WinAnsiText.Encode(text.Text, OnUnencodable.Throw, "Watermark text"),
            ExtGState = text.AlphaOverride is { } alpha ? writer.RegisterOpacity(alpha) : null,
        });
    }
}
