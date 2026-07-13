namespace Radzen.Documents.Pdf.Emit;

// Shared polymorphic queries over the code block kinds (QrCode/Barcode). A non-code block
// answers the identity (zero size, left alignment) exactly as the replaced switches did.
internal static class CodeBlockDispatch
{
    private static readonly SizeVisitor size = new();
    private static readonly AlignmentVisitor alignment = new();

    // The barcode caption band height needs the barcode's resolved font (from the per-save
    // StyleResolution), passed as the visitor context; the width and QR size are font-independent,
    // so a null resolution (width-only callers) falls back to the authored font harmlessly.
    public static (double Width, double Height) Measure(Block block, StyleResolution? resolution = null)
        => block.Accept(size, resolution);

    public static HorizontalAlignment Alignment(Block block) => block.Accept(alignment, default);

    private sealed class SizeVisitor : BlockVisitor<StyleResolution?, (double Width, double Height)>
    {
        protected override (double Width, double Height) Default(Block block, StyleResolution? resolution) => (0, 0);

        public override (double Width, double Height) Visit(QrCode qr, StyleResolution? resolution) => (qr.Size.Point, qr.Size.Point);

        public override (double Width, double Height) Visit(Barcode barcode, StyleResolution? resolution)
            => (barcode.Width.Point, barcode.Height.Point + barcode.TextBandHeight(resolution?.BarcodeFont(barcode) ?? barcode.Font));
    }

    private sealed class AlignmentVisitor : BlockVisitor<Nothing, HorizontalAlignment>
    {
        protected override HorizontalAlignment Default(Block block, Nothing context) => HorizontalAlignment.Left;

        public override HorizontalAlignment Visit(QrCode qr, Nothing context) => qr.Alignment;

        public override HorizontalAlignment Visit(Barcode barcode, Nothing context) => barcode.Alignment;
    }
}
