using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// Shared polymorphic queries over the code block kinds (QrCode/Barcode); a non-code block
// answers the identity (zero size, left alignment).
internal static class CodeBlockDispatch
{
    private static readonly SizeVisitor size = new();
    private static readonly AlignmentVisitor alignment = new();

    // The caption band must be measured by breaking the caption exactly as CodeEmitter paints
    // it, so a wrapping value reserves every line it draws; that needs fonts + resolution. Bar
    // width and QR size depend on neither, so width-only callers may omit them.
    public static (double Width, double Height) Measure(Block block, FontCollection? fonts = null, StyleResolution? resolution = null)
        => block.Accept(size, (fonts, resolution));

    public static HorizontalAlignment Alignment(Block block) => block.Accept(alignment, default);

    public static IReadOnlyList<LineBox> CaptionLines(Barcode barcode, Font font, FontCollection fonts)
    {
        var run = new Run(barcode.Value);
        run.Font.InheritFrom(font);
        var paragraph = new Paragraph { AlignmentValue = HorizontalAlignment.Center };
        paragraph.Font.InheritFrom(font);
        paragraph.Inlines.Add(run);

        return LineBreaker.Break(paragraph, barcode.Width.Point, fonts);
    }

    private static double TextBandHeight(Barcode barcode, FontCollection? fonts, StyleResolution? resolution)
    {
        if (!barcode.ShowText || fonts is null)
        {
            return 0;
        }

        var height = 0.0;

        foreach (var line in CaptionLines(barcode, resolution?.BarcodeFont(barcode) ?? barcode.Font, fonts))
        {
            height += line.Height;
        }

        return height;
    }

    private sealed class SizeVisitor : BlockVisitor<(FontCollection? Fonts, StyleResolution? Resolution), (double Width, double Height)>
    {
        protected override (double Width, double Height) Default(Block block, (FontCollection? Fonts, StyleResolution? Resolution) context) => (0, 0);

        public override (double Width, double Height) Visit(QrCode qr, (FontCollection? Fonts, StyleResolution? Resolution) context) => (qr.Size.Point, qr.Size.Point);

        public override (double Width, double Height) Visit(Barcode barcode, (FontCollection? Fonts, StyleResolution? Resolution) context)
            => (barcode.Width.Point, barcode.Height.Point + TextBandHeight(barcode, context.Fonts, context.Resolution));
    }

    private sealed class AlignmentVisitor : BlockVisitor<Nothing, HorizontalAlignment>
    {
        protected override HorizontalAlignment Default(Block block, Nothing context) => HorizontalAlignment.Left;

        public override HorizontalAlignment Visit(QrCode qr, Nothing context) => qr.Alignment;

        public override HorizontalAlignment Visit(Barcode barcode, Nothing context) => barcode.Alignment;
    }
}
