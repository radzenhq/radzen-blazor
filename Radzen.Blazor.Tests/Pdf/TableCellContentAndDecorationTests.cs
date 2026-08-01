#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class TableCellContentAndDecorationTests
{
    private static readonly string[] PaintingOperators =
        ["S", "s", "f", "F", "f*", "B", "B*", "b", "b*", "n"];

    private static Document Author(Action<Table, Cell> configure)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));

        var row = table.Rows.Add();
        var cell = row.Cells[0];
        TableLayoutSupport.Fill(cell, "X");
        configure(table, cell);
        return document;
    }

    private static List<ContentOperation> Ops(Document document)
    {
        var reader = BuildTestSupport.Read(document);
        return ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));
    }

    private static bool HasColorOperation(List<ContentOperation> ops, string op, double r, double g, double b)
    {
        foreach (var operation in ops)
        {
            if (operation.Operator == op && operation.Operands.Count >= 3
                && Math.Abs(operation.Num(0) - r) < 0.005
                && Math.Abs(operation.Num(1) - g) < 0.005
                && Math.Abs(operation.Num(2) - b) < 0.005)
            {
                return true;
            }
        }

        return false;
    }

    private static int FirstIndex(List<ContentOperation> ops, string op)
    {
        for (var i = 0; i < ops.Count; i++)
        {
            if (ops[i].Operator == op)
            {
                return i;
            }
        }

        return -1;
    }

    [Fact]
    public void BottomOnlyBorder_EmitsSingleBottomLine_NotBox()
    {
        var document = Author((_, cell) =>
        {
            cell.Borders.Bottom.Width = 0.5;
            cell.Borders.Bottom.Style = BorderStyle.Solid;
        });

        var ops = Ops(document);

        var horizontalSegment = false;
        for (var i = 0; i + 1 < ops.Count; i++)
        {
            if (ops[i].Operator == "m" && ops[i + 1].Operator == "l"
                && ops[i].Operands.Count >= 2 && ops[i + 1].Operands.Count >= 2
                && Math.Abs(ops[i].Num(1) - ops[i + 1].Num(1)) < 0.001
                && Math.Abs(ops[i].Num(0) - ops[i + 1].Num(0)) > 1)
            {
                horizontalSegment = true;
            }
        }

        Assert.True(horizontalSegment, "bottom border is drawn as a horizontal m/l segment");

        for (var i = 0; i < ops.Count; i++)
        {
            if (ops[i].Operator != "re")
            {
                continue;
            }

            for (var j = i + 1; j < ops.Count; j++)
            {
                if (Array.IndexOf(PaintingOperators, ops[j].Operator) < 0)
                {
                    continue;
                }

                Assert.True(ops[j].Operator != "S" && ops[j].Operator != "s",
                    "bottom-only border must not stroke a full rectangle");
                break;
            }
        }
    }

    [Fact]
    public void PerEdgeBorders_EmitOwnWidthAndColor()
    {
        var document = Author((_, cell) =>
        {
            cell.Borders.Top.Width = 1;
            cell.Borders.Top.Color = Color.Red;
            cell.Borders.Top.Style = BorderStyle.Solid;
            cell.Borders.Bottom.Width = 2;
            cell.Borders.Bottom.Color = Color.Blue;
            cell.Borders.Bottom.Style = BorderStyle.Solid;
        });

        var ops = Ops(document);

        Assert.True(HasColorOperation(ops, "RG", 1, 0, 0), "top edge strokes in red");
        Assert.True(HasColorOperation(ops, "RG", 0, 0, 1), "bottom edge strokes in blue");

        var widths = new HashSet<double>();
        foreach (var op in ops)
        {
            if (op.Operator == "w" && op.Operands.Count >= 1)
            {
                widths.Add(Math.Round(op.Num(0), 3));
            }
        }

        Assert.Contains(1, widths);
        Assert.Contains(2, widths);
    }

    [Fact]
    public void TableBorders_ApplyToCellsWithUnsetEdges()
    {
        var document = Author((table, _) =>
        {
            table.Borders.Width = 1;
            table.Borders.Style = BorderStyle.Solid;
        });

        var ops = Ops(document);

        var stroked = false;
        foreach (var op in ops)
        {
            if (op.Operator == "S" || op.Operator == "s")
            {
                stroked = true;
            }
        }

        Assert.True(stroked, "table-level borders stroke the cell edges");
    }

    [Fact]
    public void DashedBorder_EmitsDashPattern()
    {
        var document = Author((_, cell) =>
        {
            cell.Borders.Bottom.Width = 1;
            cell.Borders.Bottom.Style = BorderStyle.Dashed;
        });

        var ops = Ops(document);

        var dashed = false;
        foreach (var op in ops)
        {
            if (op.Operator == "d" && op.Operands.Count > 0
                && op.Operands[0].Kind == ContentTokenKind.ArrayStart
                && op.Operands.Count > 2)
            {
                dashed = true;
            }
        }

        Assert.True(dashed, "dashed border sets a non-empty dash array via 'd'");
    }

    [Fact]
    public void CellBackground_EmitsFilledRectangleBeforeText()
    {
        var document = Author((_, cell) => cell.Background = Color.Red);

        var ops = Ops(document);

        var fill = FirstIndex(ops, "f");
        Assert.True(fill >= 0, "cell background emits a fill operator");
        Assert.True(HasColorOperation(ops, "rg", 1, 0, 0), "fill uses the background color");

        var text = FirstIndex(ops, "BT");
        Assert.True(text >= 0, "cell text is still emitted");
        Assert.True(fill < text, "background is painted before (behind) the text");
    }

    [Fact]
    public void RowBackground_EmitsFilledRectangle()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row = table.Rows.Add();
        TableLayoutSupport.Fill(row.Cells[0], "A");
        TableLayoutSupport.Fill(row.Cells[1], "B");
        row.Background = Color.Blue;

        var ops = Ops(document);

        Assert.True(FirstIndex(ops, "f") >= 0, "row background emits a fill operator");
        Assert.True(HasColorOperation(ops, "rg", 0, 0, 1), "fill uses the row background color");
    }

    [Fact]
    public void ImageInCell_EmitsImageXObjectAndDoOperator()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row = table.Rows.Add();
        TableLayoutSupport.Fill(row.Cells[0], "label");
        var image = row.Cells[1].Blocks.AddImage(
            new MemoryStream(PdfTestResources.ReadAllBytes("Images/rgb.png")));
        image.Width = Unit.FromPoint(40);
        image.Height = Unit.FromPoint(20);

        var reader = BuildTestSupport.Read(document);
        Assert.True(BuildTestSupport.ImageXObjects(reader).Count > 0,
            "cell image decodes to an image XObject");

        var ops = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));
        Assert.True(FirstIndex(ops, "Do") >= 0, "cell image is drawn via Do");
    }

    [Fact]
    public void NestedTableInCell_ContentSurvivesRoundTrip()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var outer = section.Blocks.AddTable();
        outer.Columns.Add(Unit.FromPoint(300));

        var outerRow = outer.Rows.Add();
        var host = outerRow.Cells[0];
        TableLayoutSupport.Fill(host, "OUTER");

        var inner = host.Blocks.AddTable();
        inner.Columns.Add(Unit.FromPoint(120));
        var innerRow = inner.Rows.Add();
        TableLayoutSupport.Fill(innerRow.Cells[0], "NESTED");

        var text = BuildTestSupport.Reload(document).ExtractText();
        Assert.Contains("OUTER", text, StringComparison.Ordinal);
        Assert.Contains("NESTED", text, StringComparison.Ordinal);
    }

    private static Document AuthorBandImage(bool header)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var band = header ? section.Header : section.Footer;
        var image = band.Blocks.AddImage(
            new MemoryStream(PdfTestResources.ReadAllBytes("Images/rgb.png")));
        image.Width = Unit.FromPoint(50);
        image.Height = Unit.FromPoint(25);

        BuildTestSupport.AddText(section, "body", BuildTestSupport.Latin);
        return document;
    }

    [Fact]
    public void HeaderImage_EmitsImageXObjectAndDoOperator()
    {
        var reader = BuildTestSupport.Read(AuthorBandImage(header: true));

        Assert.True(BuildTestSupport.ImageXObjects(reader).Count > 0,
            "header image decodes to an image XObject");
        var ops = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));
        Assert.True(FirstIndex(ops, "Do") >= 0, "header image is drawn via Do");
    }

    [Fact]
    public void FooterImage_EmitsImageXObjectAndDoOperator()
    {
        var reader = BuildTestSupport.Read(AuthorBandImage(header: false));

        Assert.True(BuildTestSupport.ImageXObjects(reader).Count > 0,
            "footer image decodes to an image XObject");
        var ops = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));
        Assert.True(FirstIndex(ops, "Do") >= 0, "footer image is drawn via Do");
    }
}
