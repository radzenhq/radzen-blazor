using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Codes;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class CodeSymbolDispatch
{
    private static readonly CodeSymbolShapeVisitor shapes = new();

    public static (double Width, double Height) Measure(
        Block block,
        LayoutCaptureContext capture,
        FontCollection? fonts = null,
        LoweringContext? resolution = null)
        => Shape(block)?.Size(fonts, resolution, capture) ?? (0, 0);

    public static HorizontalAlignment Alignment(Block block)
        => Shape(block)?.Alignment ?? HorizontalAlignment.Left;

    public static ImmutableArray<CodeSymbolModule> Modules(Block block)
        => Shape(block)?.Modules ?? [];

    public static ImmutableArray<LaidOutCaptionLine>? Caption(
        Block block,
        FontCollection fonts,
        LoweringContext? resolution,
        LayoutCaptureContext capture)
        => Shape(block)?.Caption(fonts, resolution, capture);

    private static CodeSymbolShape? Shape(Block block) => block.Accept(shapes, default);

    private sealed class CodeSymbolShapeVisitor : BlockVisitor<Nothing, CodeSymbolShape?>
    {
        protected override CodeSymbolShape? Default(Block block, Nothing context) => null;

        public override CodeSymbolShape? Visit(QrCode block, Nothing context) => new QrCodeShape(block);

        public override CodeSymbolShape? Visit(Barcode block, Nothing context) => new BarcodeShape(block);
    }

    private abstract class CodeSymbolShape
    {
        public abstract HorizontalAlignment Alignment { get; }

        public abstract ImmutableArray<CodeSymbolModule> Modules { get; }

        public abstract (double Width, double Height) Size(
            FontCollection? fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture);

        public abstract ImmutableArray<LaidOutCaptionLine>? Caption(
            FontCollection fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture);
    }

    private sealed class QrCodeShape(QrCode qr) : CodeSymbolShape
    {
        public override HorizontalAlignment Alignment => qr.Alignment;

        public override (double Width, double Height) Size(
            FontCollection? fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture)
            => (qr.Size.Point, qr.Size.Point);

        public override ImmutableArray<LaidOutCaptionLine>? Caption(
            FontCollection fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture)
            => null;

        public override ImmutableArray<CodeSymbolModule> Modules
        {
            get
            {
                var matrix = QrEncoder.EncodeUtf8(qr.Value, qr.ErrorCorrection);
                var count = matrix.GetLength(0);
                var quiet = Math.Max(0, qr.QuietZoneModules);
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
        }
    }

    private sealed class BarcodeShape(Barcode barcode) : CodeSymbolShape
    {
        public override HorizontalAlignment Alignment => barcode.Alignment;

        public override (double Width, double Height) Size(
            FontCollection? fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture)
            => (barcode.Width.Point, barcode.Height.Point + TextBandHeight(fonts, resolution, capture));

        public override ImmutableArray<LaidOutCaptionLine>? Caption(
            FontCollection fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture)
        {
            if (!barcode.ShowText)
            {
                return null;
            }

            var lines = Lines(fonts, resolution, capture);
            var positioned = ImmutableArray.CreateBuilder<LaidOutCaptionLine>(lines.Count);
            var y = barcode.Height.Point;
            foreach (var line in lines)
            {
                positioned.Add(new LaidOutCaptionLine { Line = line, Y = y });
                y += line.Height;
            }

            return positioned.MoveToImmutable();
        }

        public override ImmutableArray<CodeSymbolModule> Modules
        {
            get
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

        private double TextBandHeight(
            FontCollection? fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture)
        {
            if (!barcode.ShowText || fonts is null)
            {
                return 0;
            }

            var height = 0.0;

            foreach (var line in Lines(fonts, resolution, capture))
            {
                height += line.Height;
            }

            return height;
        }

        private IReadOnlyList<LineBox> Lines(
            FontCollection fonts,
            LoweringContext? resolution,
            LayoutCaptureContext capture)
        {
            var font = resolution?.BarcodeFont(barcode) ?? barcode.Font;
            var run = new Run(barcode.Value);
            run.Font.InheritFrom(font);
            var paragraph = new Paragraph { Alignment = HorizontalAlignment.Center };
            paragraph.Font.InheritFrom(font);
            paragraph.Inlines.Add(run);

            return LineLayouter.Layout(paragraph, barcode.Width.Point, fonts, capture);
        }

        private static int QuietZoneModules(BarcodeType type) => type switch
        {
            BarcodeType.Ean13 or BarcodeType.Ean8 or BarcodeType.Isbn or BarcodeType.Issn => 11,
            BarcodeType.UpcA => 9,
            _ => 10,
        };
    }
}
