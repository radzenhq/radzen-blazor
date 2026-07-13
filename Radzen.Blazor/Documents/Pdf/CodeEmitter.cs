namespace Radzen.Documents.Pdf;

// Rasterizes QR codes and barcodes into filled rectangles (one per dark module/bar) and
// emits an optional human-readable caption line under a barcode.
internal sealed class CodeEmitter(FontCollection fonts)
{
    private static readonly Color CodeBlack = Color.FromRgb(0, 0, 0);

    public void EmitCode(EmitContext context, PositionedCode positioned, double left, double top)
        => EmitCodeBlock(context, positioned.Source, left + positioned.XOffset, top - positioned.Y);

    public static double CodeWidth(Block code) => code switch
    {
        QrCode qr => qr.Size.Point,
        Barcode barcode => barcode.Width.Point,
        _ => 0,
    };

    public void EmitCodeBlock(EmitContext context, Block source, double x, double topY)
    {
        var plan = context.Plan;
        switch (source)
        {
            case QrCode qr:
                EmitQrCode(plan, qr, x, topY);
                break;
            case Barcode barcode:
                EmitBarcode(context, barcode, x, topY);
                break;
            default:
                break;
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
        var quiet = System.Math.Max(0, qr.QuietZoneModules);
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

        var font = barcode.EffectiveFont ?? barcode.Font;
        var run = new Run(barcode.Value) { EffectiveFont = font };
        var paragraph = new Paragraph { AlignmentValue = HorizontalAlignment.Center, EffectiveFont = font };
        paragraph.Inlines.Add(run);

        var textTop = topY - barcode.Height.Point;
        foreach (var box in LineBreaker.Break(paragraph, barcode.Width.Point, fonts))
        {
            context.Text.EmitLine(context, box, x, textTop, null);
            textTop -= box.Height;
        }
    }
}
