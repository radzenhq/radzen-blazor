namespace Radzen.Documents.Pdf;

// Shared polymorphic queries over the code block kinds (QrCode/Barcode). A non-code block
// answers the identity (zero size, left alignment) exactly as the replaced switches did.
internal static class CodeBlockDispatch
{
    private static readonly SizeVisitor size = new();
    private static readonly AlignmentVisitor alignment = new();

    public static (double Width, double Height) Measure(Block block) => block.Accept(size, default);

    public static HorizontalAlignment Alignment(Block block) => block.Accept(alignment, default);

    private sealed class SizeVisitor : BlockVisitor<Nothing, (double Width, double Height)>
    {
        protected override (double Width, double Height) Default(Block block, Nothing context) => (0, 0);

        public override (double Width, double Height) Visit(QrCode qr, Nothing context) => (qr.Size.Point, qr.Size.Point);

        public override (double Width, double Height) Visit(Barcode barcode, Nothing context) => (barcode.Width.Point, barcode.Height.Point + barcode.TextBandHeight);
    }

    private sealed class AlignmentVisitor : BlockVisitor<Nothing, HorizontalAlignment>
    {
        protected override HorizontalAlignment Default(Block block, Nothing context) => HorizontalAlignment.Left;

        public override HorizontalAlignment Visit(QrCode qr, Nothing context) => qr.Alignment;

        public override HorizontalAlignment Visit(Barcode barcode, Nothing context) => barcode.Alignment;
    }
}
