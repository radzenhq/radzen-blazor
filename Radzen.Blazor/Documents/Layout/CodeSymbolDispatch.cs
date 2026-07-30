using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Codes;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class CodeSymbolDispatch
{
    private static readonly SizeVisitor size = new();
    private static readonly AlignmentVisitor alignment = new();

    public static (double Width, double Height) Measure(Block block, FontCollection? fonts = null, LoweringContext? resolution = null)
        => block.Accept(size, (fonts, resolution));

    public static HorizontalAlignment Alignment(Block block) => block.Accept(alignment, default);

    private static readonly ModuleVisitor modules = new();

    public static ImmutableArray<CodeSymbolModule> Modules(Block block) => block.Accept(modules, default);

    public static IReadOnlyList<LineBox> CaptionLines(Barcode barcode, Font font, FontCollection fonts)
    {
        var run = new Run(barcode.Value);
        run.Font.InheritFrom(font);
        var paragraph = new Paragraph { Alignment = HorizontalAlignment.Center };
        paragraph.Font.InheritFrom(font);
        paragraph.Inlines.Add(run);

        return LineBreaker.Break(paragraph, barcode.Width.Point, fonts);
    }

    public static ImmutableArray<PositionedCaptionLine>? Caption(Block block, FontCollection fonts, LoweringContext? resolution)
    {
        if (block is not Barcode barcode || !barcode.ShowText)
        {
            return null;
        }

        var lines = CaptionLines(barcode, resolution?.BarcodeFont(barcode) ?? barcode.Font, fonts);
        var positioned = ImmutableArray.CreateBuilder<PositionedCaptionLine>(lines.Count);
        var y = barcode.Height.Point;
        foreach (var line in lines)
        {
            positioned.Add(new PositionedCaptionLine { Line = line, Y = y });
            y += line.Height;
        }

        return positioned.MoveToImmutable();
    }

    private static double TextBandHeight(Barcode barcode, FontCollection? fonts, LoweringContext? resolution)
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

    private sealed class SizeVisitor : BlockVisitor<(FontCollection? Fonts, LoweringContext? Resolution), (double Width, double Height)>
    {
        protected override (double Width, double Height) Default(Block block, (FontCollection? Fonts, LoweringContext? Resolution) context) => (0, 0);

        public override (double Width, double Height) Visit(QrCode qr, (FontCollection? Fonts, LoweringContext? Resolution) context) => (qr.Size.Point, qr.Size.Point);

        public override (double Width, double Height) Visit(Barcode barcode, (FontCollection? Fonts, LoweringContext? Resolution) context)
            => (barcode.Width.Point, barcode.Height.Point + TextBandHeight(barcode, context.Fonts, context.Resolution));
    }

    private static int QuietZoneModules(BarcodeType type) => type switch
    {
        BarcodeType.Ean13 or BarcodeType.Ean8 or BarcodeType.Isbn or BarcodeType.Issn => 11,
        BarcodeType.UpcA => 9,
        _ => 10,
    };

    private sealed class ModuleVisitor : BlockVisitor<Nothing, ImmutableArray<CodeSymbolModule>>
    {
        protected override ImmutableArray<CodeSymbolModule> Default(Block block, Nothing context) => [];

        public override ImmutableArray<CodeSymbolModule> Visit(QrCode qr, Nothing context)
        {
            var matrix = QrEncoder.EncodeUtf8(qr.Value, qr.ErrorCorrection);
            var count = matrix.GetLength(0);
            var quiet = System.Math.Max(0, qr.QuietZoneModules);
            var module = qr.Size.Point / (count + (2 * quiet));

            var result = ImmutableArray.CreateBuilder<CodeSymbolModule>();
            for (var row = 0; row < count; row++)
            {
                for (var column = 0; column < count; column++)
                {
                    if (matrix[row, column])
                    {
                        result.Add(new CodeSymbolModule
                        {
                            X = (quiet + column) * module,
                            Y = (quiet + row) * module,
                            Width = module,
                            Height = module,
                        });
                    }
                }
            }

            return result.ToImmutable();
        }

        public override ImmutableArray<CodeSymbolModule> Visit(Barcode barcode, Nothing context)
        {
            var quiet = QuietZoneModules(barcode.Type);
            var (bars, moduleCount, _) = BarcodeEncoder.EncodeToBars(
                barcode.Type, barcode.Value, barcode.Height.Point, quiet);
            var scaleX = barcode.Width.Point / moduleCount;

            var result = ImmutableArray.CreateBuilder<CodeSymbolModule>(bars.Count);
            foreach (var bar in bars)
            {
                result.Add(new CodeSymbolModule
                {
                    X = bar.X * scaleX,
                    Y = bar.Y,
                    Width = bar.Width * scaleX,
                    Height = bar.Height,
                });
            }

            return result.MoveToImmutable();
        }
    }

    private sealed class AlignmentVisitor : BlockVisitor<Nothing, HorizontalAlignment>
    {
        protected override HorizontalAlignment Default(Block block, Nothing context) => HorizontalAlignment.Left;

        public override HorizontalAlignment Visit(QrCode qr, Nothing context) => qr.Alignment;

        public override HorizontalAlignment Visit(Barcode barcode, Nothing context) => barcode.Alignment;
    }
}
