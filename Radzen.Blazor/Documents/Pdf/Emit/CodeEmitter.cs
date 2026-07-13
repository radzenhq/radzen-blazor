using System;

namespace Radzen.Documents.Pdf.Emit;

// Rasterizes QR codes and barcodes into filled rectangles (one per dark module/bar) and
// emits an optional human-readable caption line under a barcode.
internal sealed class CodeEmitter(FontCollection fonts, StyleResolution resolution)
{
    private static readonly Color CodeBlack = Color.FromRgb(0, 0, 0);

    public void EmitCode(EmitContext context, PositionedCode positioned, double left, double top)
        => EmitCodeBlock(context, positioned.Source, left + positioned.XOffset, top - positioned.Y);

    public static double CodeWidth(Block code) => CodeBlockDispatch.Measure(code).Width;

    private EmitVisitor? emitVisitor;

    public void EmitCodeBlock(EmitContext context, Block source, double x, double topY)
        => source.Accept(emitVisitor ??= new EmitVisitor(this), (context, x, topY));

    // Rasterizes a code block; a non-code block emits nothing (Default).
    private sealed class EmitVisitor(CodeEmitter owner) : BlockVisitor<(EmitContext Context, double X, double TopY), Nothing>
    {
        protected override Nothing Default(Block block, (EmitContext Context, double X, double TopY) args) => default;

        public override Nothing Visit(QrCode qr, (EmitContext Context, double X, double TopY) args)
        {
            EmitQrCode(args.Context.Plan, qr, args.X, args.TopY);
            return default;
        }

        public override Nothing Visit(Barcode barcode, (EmitContext Context, double X, double TopY) args)
        {
            owner.EmitBarcode(args.Context, barcode, args.X, args.TopY);
            return default;
        }
    }

    // Spec-mandated minimum quiet zone (light margin) each side, in modules, so 1D symbols
    // stay scannable. EAN wants 11x, UPC-A 9x; Code128 and the rest need at least 10x.
    private static int QuietZoneModules(BarcodeType type) => type switch
    {
        BarcodeType.Ean13 or BarcodeType.Ean8 or BarcodeType.Isbn or BarcodeType.Issn => 11,
        BarcodeType.UpcA => 9,
        _ => 10,
    };

    // One filled square per dark module, scaled so matrix plus quiet zone fits Size x Size.
    private static void EmitQrCode(PagePlan plan, QrCode qr, double x, double topY)
    {
        var matrix = Radzen.Documents.QrEncoder.EncodeUtf8(qr.Value, qr.ErrorCorrection);
        var modules = matrix.GetLength(0);
        var quiet = Math.Max(0, qr.QuietZoneModules);
        var module = qr.Size.Point / (modules + (2 * quiet));

        for (var row = 0; row < modules; row++)
        {
            for (var column = 0; column < modules; column++)
            {
                if (!matrix[row, column])
                {
                    continue;
                }

                plan.Fills.Add(new FillDraw
                {
                    X = x + ((quiet + column) * module),
                    Y = topY - ((quiet + row + 1) * module),
                    Width = module,
                    Height = module,
                    Color = CodeBlack,
                });
            }
        }
    }

    // Bars come back with X/Width in modules and Y/Height in points, so only X scales.
    private void EmitBarcode(EmitContext context, Barcode barcode, double x, double topY)
    {
        var plan = context.Plan;
        var quiet = QuietZoneModules(barcode.Type);
        var (bars, moduleCount, _) = Radzen.Documents.BarcodeEncoder.EncodeToBars(barcode.Type, barcode.Value, barcode.Height.Point, quiet);
        var scaleX = barcode.Width.Point / moduleCount;

        foreach (var bar in bars)
        {
            plan.Fills.Add(new FillDraw
            {
                X = x + (bar.X * scaleX),
                Y = topY - bar.Y - bar.Height,
                Width = bar.Width * scaleX,
                Height = bar.Height,
                Color = CodeBlack,
            });
        }

        if (!barcode.ShowText)
        {
            return;
        }

        var font = resolution.BarcodeFont(barcode) ?? barcode.Font;
        // The synthesized caption run/paragraph carry the resolved barcode font as their authored
        // font, so the line breaker reads exactly the font the caption is measured and drawn with.
        var run = new Run(barcode.Value);
        run.Font.InheritFrom(font);
        var paragraph = new Paragraph { AlignmentValue = HorizontalAlignment.Center };
        paragraph.Font.InheritFrom(font);
        paragraph.Inlines.Add(run);

        var textTop = topY - barcode.Height.Point;
        foreach (var box in LineBreaker.Break(paragraph, barcode.Width.Point, fonts))
        {
            context.Text.EmitLine(context, box, x, textTop, null);
            textTop -= box.Height;
        }
    }
}
