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

// QR and barcode block elements rendered as vector rectangle fills in the page content.
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
    public void Barcode_Code128_EmitsBarRects_SpanningDeclaredWidth()
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
        Assert.Equal(200.0, span, 2);
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
        var packet = Encoding.UTF8.GetString(metadata.Data);
        Assert.Contains("<pdfaid:part>3</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", packet, StringComparison.Ordinal);

        Assert.NotEmpty(FilledRects(ContentTestHelpers.PageContent(reader, 0)));
    }
}
