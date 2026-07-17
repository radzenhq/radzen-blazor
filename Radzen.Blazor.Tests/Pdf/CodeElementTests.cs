#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class CodeElementTests
{
    private static List<(double X, double Y, double W, double H)> FilledRects(byte[] content)
    {
        var ops = ContentStreamTokenizer.Parse(content);
        var rects = new List<(double, double, double, double)>();
        for (var i = 0; i + 1 < ops.Count; i++)
        {
            if (ops[i].Operator == "re" && ops[i + 1].Operator == "f")
            {
                rects.Add((ops[i].Num(0), ops[i].Num(1), ops[i].Num(2), ops[i].Num(3)));
            }
        }

        return rects;
    }

    private static int DarkModules(bool[,] matrix)
    {
        var count = 0;
        for (var r = 0; r < matrix.GetLength(0); r++)
        {
            for (var c = 0; c < matrix.GetLength(1); c++)
            {
                if (matrix[r, c])
                {
                    count++;
                }
            }
        }

        return count;
    }

    [Fact]
    public void QrCode_EmitsOneFilledSquarePerDarkModule_WithinDeclaredSize()
    {
        const string value = "https://radzen.com";
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var qr = section.Blocks.AddQrCode(value, Unit.FromPoint(120));
        Assert.Same(qr, section.Blocks[0]);

        var matrix = QrEncoder.EncodeUtf8(value, QrErrorCorrection.Medium);
        var expectedModule = 120.0 / (matrix.GetLength(0) + 8);

        var reader = BuildTestSupport.Read(builder);
        var rects = FilledRects(ContentTestHelpers.PageContent(reader, 0));

        Assert.Equal(DarkModules(matrix), rects.Count);
        Assert.All(rects, r =>
        {
            Assert.Equal(expectedModule, r.W, 3);
            Assert.Equal(expectedModule, r.H, 3);
        });

        var spanX = rects.Max(r => r.X + r.W) - rects.Min(r => r.X);
        var spanY = rects.Max(r => r.Y + r.H) - rects.Min(r => r.Y);
        Assert.True(spanX <= 120 + 0.01, $"QR width {spanX} exceeds declared size");
        Assert.True(spanY <= 120 + 0.01, $"QR height {spanY} exceeds declared size");
    }

    [Fact]
    public void QrCode_HigherErrorCorrection_ChangesEmittedModules()
    {
        const string value = "error correction";
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.AddQrCode(value, Unit.FromPoint(100), QrErrorCorrection.High);

        var matrix = QrEncoder.EncodeUtf8(value, QrErrorCorrection.High);
        var reader = BuildTestSupport.Read(builder);
        var rects = FilledRects(ContentTestHelpers.PageContent(reader, 0));

        Assert.Equal(DarkModules(matrix), rects.Count);
    }

    [Fact]
    public void Barcode_Code128_EmitsBarRects_WithinDeclaredWidth()
    {
        const string value = "RADZEN";
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var barcode = section.Blocks.AddBarcode(BarcodeType.Code128, value, Unit.FromPoint(200), Unit.FromPoint(40));
        Assert.Same(barcode, section.Blocks[0]);

        var widths = BarcodeEncoder.EncodeCode128B(value);
        var expectedBars = 0;
        var isBar = true;
        foreach (var w in widths)
        {
            if (isBar && w > 0)
            {
                expectedBars++;
            }

            isBar = !isBar;
        }

        var reader = BuildTestSupport.Read(builder);
        var rects = FilledRects(ContentTestHelpers.PageContent(reader, 0));

        Assert.Equal(expectedBars, rects.Count);
        Assert.All(rects, r => Assert.Equal(40.0, r.H, 3));

        var span = rects.Max(r => r.X + r.W) - rects.Min(r => r.X);
        Assert.True(span < 200.0, $"bars span {span} should be narrower than the declared width once quiet zones are added");
    }

    [Fact]
    public void Barcode_Code128_LeavesSpecQuietZoneWithinDeclaredWidth()
    {
        const string value = "RADZEN";
        const double width = 200.0;
        const int quiet = 10;
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.AddBarcode(BarcodeType.Code128, value, Unit.FromPoint(width), Unit.FromPoint(40));

        var (_, symbolModules, _) = BarcodeEncoder.EncodeToBars(BarcodeType.Code128, value, 40, 0);
        var total = symbolModules + 2 * quiet;
        var scaleX = width / total;

        var reader = BuildTestSupport.Read(builder);
        var rects = FilledRects(ContentTestHelpers.PageContent(reader, 0));

        var barsSpan = rects.Max(r => r.X + r.W) - rects.Min(r => r.X);
        var totalQuiet = width - barsSpan;

        Assert.Equal(symbolModules * scaleX, barsSpan, 2);
        Assert.Equal(2 * quiet * scaleX, totalQuiet, 2);
    }

    [Fact]
    public void Barcode_ShowText_RendersValueBelowBars()
    {
        const string value = "RADZEN";
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.AddBarcode(BarcodeType.Code128, value, Unit.FromPoint(200), Unit.FromPoint(40), showText: true);

        var reader = BuildTestSupport.Read(builder);
        var content = ContentTestHelpers.PageContent(reader, 0);
        var ops = ContentStreamTokenizer.Parse(content);

        var texts = ops.Where(o => o.Operator == "Tj")
            .Select(o => Encoding.ASCII.GetString(o.Operands[0].Bytes))
            .ToList();
        Assert.Contains(value, texts);

        var barsBottom = FilledRects(content).Min(r => r.Y);
        var baseline = ops.First(o => o.Operator == "Td").Num(1);
        Assert.True(baseline < barsBottom, $"text baseline {baseline} is not below the bars bottom {barsBottom}");
    }

    private static List<(string Text, double Y)> ShownText(byte[] content)
    {
        var ops = ContentStreamTokenizer.Parse(content);
        var shown = new List<(string, double)>();
        var y = 0.0;
        foreach (var op in ops)
        {
            if (op.Operator == "Td")
            {
                y = op.Num(1);
            }
            else if (op.Operator == "Tj")
            {
                shown.Add((Encoding.ASCII.GetString(op.Operands[0].Bytes), y));
            }
        }

        return shown;
    }

    [Fact]
    public void Barcode_ShowText_WrappedCaption_DoesNotOverlapNextBlock()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN CODE ONE TWO", Unit.FromPoint(50), Unit.FromPoint(40), showText: true);
        section.Blocks.AddParagraph("after");

        var shown = ShownText(ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0));
        var after = shown.Single(s => s.Text == "after").Y;
        var caption = shown.Where(s => s.Text != "after").ToList();

        Assert.True(caption.Count > 1, "the caption did not wrap");
        Assert.All(caption, line => Assert.True(line.Y > after, $"caption line '{line.Text}' at {line.Y} overlaps the next block at {after}"));
    }

    [Fact]
    public void Barcode_ShowText_WithoutExplicitFont_UsesDocumentDefaultFont_InPdfA()
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3B };
        builder.Info.Title = "Barcode";
        BuildTestSupport.RegisterLatin(builder);
        builder.Styles.Normal.Font.Name = BuildTestSupport.Latin;
        var section = builder.Sections.Add();
        section.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN", Unit.FromPoint(200), Unit.FromPoint(40), showText: true);

        var reader = BuildTestSupport.Read(builder);

        Assert.DoesNotContain(BuildTestSupport.Fonts(reader), f =>
            f.TryGetValue("BaseFont", out var baseFont)
            && reader.Resolve(baseFont!) is NameObject name
            && name.Value == "Helvetica");
        Assert.NotEmpty(BuildTestSupport.Type0Fonts(reader));
    }

    [Fact]
    public void Barcode_ShowText_WithoutExplicitFont_InheritsDefaultFontSize()
    {
        var builder = new DocumentBuilder();
        builder.Styles.Normal.Font.Size = 14;
        var section = builder.Sections.Add();
        section.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN", Unit.FromPoint(200), Unit.FromPoint(40), showText: true);

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(14.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void Barcode_ShowText_ExplicitFont_WinsOverDefault()
    {
        var builder = new DocumentBuilder();
        builder.Styles.Normal.Font.Size = 14;
        var section = builder.Sections.Add();
        var barcode = section.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN", Unit.FromPoint(200), Unit.FromPoint(40), showText: true);
        barcode.Font.Name = "Courier";
        barcode.Font.Size = 8;

        var reader = BuildTestSupport.Read(builder);
        var content = CascadeTestSupport.FirstPageContent(builder);

        Assert.Contains(8.0, CascadeTestSupport.TfSizes(content));
        Assert.Contains(BuildTestSupport.Fonts(reader), f =>
            f.TryGetValue("BaseFont", out var baseFont)
            && reader.Resolve(baseFont!) is NameObject name
            && name.Value == "Courier");
    }

    [Fact]
    public void QrCode_InTableCell_EmitsModuleRects_AndReservesRowHeight()
    {
        const string value = "cell qr";
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Rows.Add().Cells[0].Blocks.AddQrCode(value, Unit.FromPoint(90));
        table.Rows.Add().Cells[0].Text = "below";

        var matrix = QrEncoder.EncodeUtf8(value, QrErrorCorrection.Medium);
        var reader = BuildTestSupport.Read(builder);
        var content = ContentTestHelpers.PageContent(reader, 0);
        var rects = FilledRects(content);

        Assert.Equal(DarkModules(matrix), rects.Count);

        var qrBottom = rects.Min(r => r.Y);
        var textBaseline = CascadeTestSupport.TdPositions(Encoding.Latin1.GetString(content)).Max(p => p.Y);
        Assert.True(textBaseline < qrBottom, $"second-row text baseline {textBaseline} is not below the QR bottom {qrBottom}");
    }

    [Fact]
    public void Barcode_InTableCell_EmitsBarRects()
    {
        const string value = "RADZEN";
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Rows.Add().Cells[0].Blocks.AddBarcode(BarcodeType.Code128, value, Unit.FromPoint(200), Unit.FromPoint(40));

        var widths = BarcodeEncoder.EncodeCode128B(value);
        var expectedBars = 0;
        var isBar = true;
        foreach (var w in widths)
        {
            if (isBar && w > 0)
            {
                expectedBars++;
            }

            isBar = !isBar;
        }

        var reader = BuildTestSupport.Read(builder);
        var rects = FilledRects(ContentTestHelpers.PageContent(reader, 0));

        Assert.Equal(expectedBars, rects.Count);
        Assert.All(rects, r => Assert.Equal(40.0, r.H, 3));
    }

    [Fact]
    public void Document_WithQrCodeAndBarcode_KeepsPdfA3BConformance()
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3B };
        builder.Info.Title = "Codes";
        var section = builder.Sections.Add();
        section.Blocks.AddQrCode("PDF/A", Unit.FromPoint(80));
        section.Blocks.AddBarcode(BarcodeType.Code128, "PDFA", Unit.FromPoint(160), Unit.FromPoint(30));

        var reader = BuildTestSupport.Read(builder);

        var root = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));
        var metadata = Assert.IsType<StreamObject>(reader.Resolve(root["Metadata"]));
        var packet = Encoding.UTF8.GetString(metadata.Data.ToArray());
        Assert.Contains("<pdfaid:part>3</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", packet, StringComparison.Ordinal);

        Assert.NotEmpty(FilledRects(ContentTestHelpers.PageContent(reader, 0)));
    }
}
