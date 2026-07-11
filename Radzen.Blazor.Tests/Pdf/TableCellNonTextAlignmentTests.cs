#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Non-text cell content (image, QR, barcode) must honor its OWN Alignment and, when the
// block leaves it at the default Left, the cell/column alignment - resolving End the same
// way text does (flush right) rather than folding it to left.
public class TableCellNonTextAlignmentTests
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

    private static (DocumentBuilder Builder, Table Table, Column Column) WideSingleColumn()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(500));
        section.Margin = Unit.FromPoint(40);
        var table = section.Blocks.AddTable();
        var column = table.Columns.Add();
        return (builder, table, column);
    }

    private static double QrMinX(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        return FilledRects(ContentTestHelpers.PageContent(reader, 0)).Min(r => r.X);
    }

    [Fact]
    public void QrCode_RightAlignment_ShiftsRightVersusDefaultLeft()
    {
        var (left, leftTable, _) = WideSingleColumn();
        leftTable.Rows.Add().Cells[0].Blocks.AddQrCode("qr", Unit.FromPoint(90));

        var (rightBuilder, rightTable, _) = WideSingleColumn();
        var qr = rightTable.Rows.Add().Cells[0].Blocks.AddQrCode("qr", Unit.FromPoint(90));
        qr.Alignment = HorizontalAlignment.Right;

        Assert.True(QrMinX(rightBuilder) > QrMinX(left) + 100,
            "a Right-aligned QR must sit well to the right of the default left-aligned QR");
    }

    [Fact]
    public void Image_RightAlignment_ShiftsRightVersusDefaultLeft()
    {
        static double ImageX(DocumentBuilder builder)
        {
            var reader = BuildTestSupport.Read(builder);
            var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, 0));
            var match = Regex.Match(content, @"50(?:\.0+)?\s+0\S*\s+0\S*\s+\d+(?:\.\d+)?\s+([-\d.]+)\s+[-\d.]+\s+cm");
            Assert.True(match.Success, "image cm not found");
            return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        var (left, leftTable, _) = WideSingleColumn();
        var leftImage = leftTable.Rows.Add().Cells[0].Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        leftImage.Width = Unit.FromPoint(50);
        leftImage.Height = Unit.FromPoint(40);

        var (rightBuilder, rightTable, _) = WideSingleColumn();
        var rightImage = rightTable.Rows.Add().Cells[0].Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        rightImage.Width = Unit.FromPoint(50);
        rightImage.Height = Unit.FromPoint(40);
        rightImage.Alignment = HorizontalAlignment.Right;

        Assert.True(ImageX(rightBuilder) > ImageX(left) + 100,
            "a Right-aligned image must sit well to the right of the default left-aligned image");
    }

    [Fact]
    public void ColumnEndAlignment_RightAlignsBothTextAndQr()
    {
        var (builder, table, column) = WideSingleColumn();
        column.Alignment = HorizontalAlignment.End;
        table.Rows.Add().Cells[0].Blocks.AddParagraph("hi");
        table.Rows.Add().Cells[0].Blocks.AddQrCode("qr", Unit.FromPoint(90));

        var reader = BuildTestSupport.Read(builder);
        var content = ContentTestHelpers.PageContent(reader, 0);

        var qrRight = FilledRects(content).Max(r => r.X + r.W);
        var textX = CascadeTestSupport.TdPositions(Encoding.Latin1.GetString(content)).Max(p => p.X);

        // Both the QR and the text must flush right (past the page midpoint at x=200),
        // where previously the QR folded left while text went right.
        Assert.True(qrRight > 200, $"End-aligned QR right edge {qrRight} should be flush right");
        Assert.True(textX > 200, $"End-aligned text x {textX} should be flush right");
    }
}
