#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class TableCellSpacingAlignmentIndentRegressionTests
{
    private const double Tol = 0.5;

    private static byte[] PageBytes(Document document)
        => ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0);

    private static List<ContentOperation> Ops(Document document)
        => ContentStreamTokenizer.Parse(PageBytes(document));

    private static List<(string Text, double X, double Y)> TextRuns(Document document)
    {
        var content = Encoding.Latin1.GetString(PageBytes(document));
        var runs = new List<(string, double, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            runs.Add((m.Groups[3].Value,
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return runs;
    }

    private static (double X, double Y) Run(Document document, string text)
    {
        var run = TextRuns(document).Single(r => r.Text == text);
        return (run.X, run.Y);
    }

    private static List<(double X1, double Y1, double X2, double Y2)> Segments(List<ContentOperation> ops)
    {
        var segments = new List<(double, double, double, double)>();
        for (var i = 0; i + 1 < ops.Count; i++)
        {
            if (ops[i].Operator == "m" && ops[i + 1].Operator == "l"
                && ops[i].Operands.Count >= 2 && ops[i + 1].Operands.Count >= 2)
            {
                segments.Add((ops[i].Num(0), ops[i].Num(1), ops[i + 1].Num(0), ops[i + 1].Num(1)));
            }
        }

        return segments;
    }

    private static List<(double X1, double Y1, double X2, double Y2)> VerticalSegments(Document document)
        => Segments(Ops(document))
            .Where(s => Math.Abs(s.X1 - s.X2) < 0.001 && Math.Abs(s.Y1 - s.Y2) > 1)
            .ToList();

    private static (Document Builder, Section Section) NewDocument()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(600));
        section.Margins.SetAll(Unit.FromPoint(50));
        return (document, section);
    }


    private static Document TwoParagraphCell(double spacingBefore)
    {
        var (document, section) = NewDocument();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        cell.Blocks.AddParagraph("P1");
        var second = cell.Blocks.AddParagraph("P2");
        second.SpacingBefore = Unit.FromPoint(spacingBefore);
        return document;
    }

    [Fact]
    public void SpacingBefore_SeparatesParagraphsInsideCell()
    {
        var plain = TwoParagraphCell(0);
        var spaced = TwoParagraphCell(20);

        var plainGap = Run(plain, "P1").Y - Run(plain, "P2").Y;
        var spacedGap = Run(spaced, "P1").Y - Run(spaced, "P2").Y;

        Assert.Equal(20, spacedGap - plainGap, Tol);
    }

    private static Document TwoRowTable(double spacingAfter)
    {
        var (document, section) = NewDocument();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var first = table.Rows.Add().Cells[0].Blocks.AddParagraph("A");
        first.SpacingAfter = Unit.FromPoint(spacingAfter);
        table.Rows.Add().Cells[0].Blocks.AddParagraph("B");
        return document;
    }

    [Fact]
    public void SpacingAfter_GrowsRowHeight_PushingNextRowDown()
    {
        var plain = TwoRowTable(0);
        var spaced = TwoRowTable(30);

        var plainGap = Run(plain, "A").Y - Run(plain, "B").Y;
        var spacedGap = Run(spaced, "A").Y - Run(spaced, "B").Y;

        Assert.Equal(30, spacedGap - plainGap, Tol);
    }

    [Fact]
    public void SpacingBetweenCellParagraphs_GrowsRowHeight_PushingContentAfterTableDown()
    {
        static Document Author(double spacingBefore)
        {
            var document = TwoParagraphCell(spacingBefore);
            document.Sections[0].Blocks.AddParagraph("After");
            return document;
        }

        var plain = Author(0);
        var spaced = Author(20);

        var delta = Run(plain, "After").Y - Run(spaced, "After").Y;

        Assert.Equal(20, delta, Tol);
    }


    private static Table OneCellTable(Section section, string text)
    {
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        table.Rows.Add().Cells[0].Blocks.AddParagraph(text);
        return table;
    }

    [Fact]
    public void CellAlignment_BeatsColumnAlignment()
    {
        var (document, section) = NewDocument();
        var table = OneCellTable(section, "RightMe");
        table.Columns[0].Alignment = HorizontalAlignment.Left;
        table.Rows[0].Cells[0].Alignment = HorizontalAlignment.Right;

        var (reference, referenceSection) = NewDocument();
        var referenceTable = OneCellTable(referenceSection, "RightMe");
        referenceTable.Rows[0].Cells[0].Alignment = HorizontalAlignment.Right;

        var expected = Run(reference, "RightMe").X;
        Assert.True(expected > 55, "reference cell-aligned text must sit right of the left content edge");
        Assert.Equal(expected, Run(document, "RightMe").X, Tol);
    }

    [Fact]
    public void NamedStyleAlignment_BeatsAlignmentInheritedFromRow()
    {
        var (document, section) = NewDocument();
        document.Styles.Add("Numeric").Alignment = HorizontalAlignment.Right;
        var table = OneCellTable(section, "RightMe");
        table.Rows[0].Alignment = HorizontalAlignment.Center;
        ((Paragraph)table.Rows[0].Cells[0].Blocks[0]).StyleName = "Numeric";

        var (reference, referenceSection) = NewDocument();
        var referenceTable = OneCellTable(referenceSection, "RightMe");
        ((Paragraph)referenceTable.Rows[0].Cells[0].Blocks[0]).Alignment = HorizontalAlignment.Right;

        var expected = Run(reference, "RightMe").X;
        Assert.True(expected > 55, "reference right-aligned text must sit right of the left content edge");
        Assert.Equal(expected, Run(document, "RightMe").X, Tol);
    }

    [Fact]
    public void NamedStyleAlignment_BeatsAlignmentInheritedFromColumn()
    {
        var (document, section) = NewDocument();
        document.Styles.Add("Middle").Alignment = HorizontalAlignment.Center;
        var table = OneCellTable(section, "MidMe");
        table.Columns[0].Alignment = HorizontalAlignment.Left;
        ((Paragraph)table.Rows[0].Cells[0].Blocks[0]).StyleName = "Middle";

        var (reference, referenceSection) = NewDocument();
        var referenceTable = OneCellTable(referenceSection, "MidMe");
        ((Paragraph)referenceTable.Rows[0].Cells[0].Blocks[0]).Alignment = HorizontalAlignment.Center;

        var expected = Run(reference, "MidMe").X;
        Assert.True(expected > 55, "reference centered text must sit right of the left content edge");
        Assert.Equal(expected, Run(document, "MidMe").X, Tol);
    }


    private static void AssertIndentedEdges(Document document)
    {
        var vertical = VerticalSegments(document);
        Assert.True(vertical.Count > 0, "table borders with Width > 0 must draw vertical edges");

        var minX = vertical.Min(s => s.X1);
        var maxX = vertical.Max(s => s.X1);

        Assert.Equal(150, minX, Tol);
        Assert.True(maxX <= 350 + Tol, $"indented auto table must not cross the right content edge (right edge at {maxX})");
    }

    [Fact]
    public void IndentedAutoWidthTable_InBody_StaysInsideRightContentEdge()
    {
        var (document, section) = NewDocument();
        var table = OneCellTable(section, "X");
        table.Columns[0].Width = null;
        table.LeftIndent = Unit.FromPoint(100);
        table.Borders.Width = 1;

        AssertIndentedEdges(document);
    }

    [Fact]
    public void IndentedAutoWidthTable_InHeaderBand_StaysInsideRightContentEdge()
    {
        var (document, section) = NewDocument();
        section.Blocks.AddParagraph("Body");

        var table = section.Header.Blocks.AddTable();
        table.Columns.Add();
        table.Rows.Add().Cells[0].Blocks.AddParagraph("H");
        table.LeftIndent = Unit.FromPoint(100);
        table.Borders.Width = 1;

        AssertIndentedEdges(document);
    }

    [Fact]
    public void IndentedAutoWidthNestedTable_StaysInsideOuterCell()
    {
        var (document, section) = NewDocument();
        var outer = section.Blocks.AddTable();
        outer.Columns.Add(Unit.FromPoint(200));
        var cell = outer.Rows.Add().Cells[0];

        var nested = cell.Blocks.AddTable();
        nested.Columns.Add();
        nested.Rows.Add().Cells[0].Blocks.AddParagraph("N");
        nested.LeftIndent = Unit.FromPoint(80);
        nested.Borders.Width = 1;

        var vertical = VerticalSegments(document);
        Assert.True(vertical.Count > 0, "nested table borders must draw vertical edges");

        var minX = vertical.Min(s => s.X1);
        var maxX = vertical.Max(s => s.X1);

        Assert.Equal(130, minX, Tol);
        Assert.True(maxX <= 250 + Tol, $"indented nested table must not cross the outer cell right edge (right edge at {maxX})");
    }
}
