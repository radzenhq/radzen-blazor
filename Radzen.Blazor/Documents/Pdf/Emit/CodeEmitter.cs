using System;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class CodeEmitter(FontCollection fonts, StyleResolution resolution)
{
    private static readonly Color CodeBlack = Color.FromRgb(0, 0, 0);

    public void EmitCode(EmitContext context, PositionedCode positioned, double left, double top)
        => EmitCodeBlock(context, positioned.Source, left + positioned.XOffset, top - positioned.Y);

    public static double CodeWidth(Block code) => CodeBlockDispatch.Measure(code).Width;

    private EmitVisitor? emitVisitor;

    public void EmitCodeBlock(EmitContext context, Block source, double x, double topY)
        => source.Accept(emitVisitor ??= new EmitVisitor(this), (context, x, topY));

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

    private static int QuietZoneModules(BarcodeType type) => type switch
    {
        BarcodeType.Ean13 or BarcodeType.Ean8 or BarcodeType.Isbn or BarcodeType.Issn => 11,
        BarcodeType.UpcA => 9,
        _ => 10,
    };

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
        var textTop = topY - barcode.Height.Point;
        foreach (var box in CodeBlockDispatch.CaptionLines(barcode, font, fonts))
        {
            context.Text.EmitLine(context, box, x, textTop, null);
            textTop -= box.Height;
        }
    }
}
