#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TableCellSpacingAlignmentIndentRegressionTests
{
    private const double Tol = 0.5;

    private static byte[] PageBytes(DocumentBuilder builder)
        => ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0);

    private static List<ContentOperation> Ops(DocumentBuilder builder)
        => ContentStreamTokenizer.Parse(PageBytes(builder));

    private static List<(string Text, double X, double Y)> TextRuns(DocumentBuilder builder)
    {
        var content = Encoding.Latin1.GetString(PageBytes(builder));
        var runs = new List<(string, double, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            runs.Add((m.Groups[3].Value,
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return runs;
    }

    private static (double X, double Y) Run(DocumentBuilder builder, string text)
    {
        var run = TextRuns(builder).Single(r => r.Text == text);
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

    private static List<(double X1, double Y1, double X2, double Y2)> VerticalSegments(DocumentBuilder builder)
        => Segments(Ops(builder))
            .Where(s => Math.Abs(s.X1 - s.X2) < 0.001 && Math.Abs(s.Y1 - s.Y2) > 1)
            .ToList();

    private static (DocumentBuilder Builder, Section Section) NewDocument()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(600));
        section.Margin = Unit.FromPoint(50);
        return (builder, section);
    }


    private static DocumentBuilder TwoParagraphCell(double spacingBefore)
    {
        var (builder, section) = NewDocument();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var cell = table.Rows.Add().Cells[0];
        cell.Blocks.AddParagraph("P1");
        var second = cell.Blocks.AddParagraph("P2");
        second.SpacingBefore = Unit.FromPoint(spacingBefore);
        return builder;
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

    private static DocumentBuilder TwoRowTable(double spacingAfter)
    {
        var (builder, section) = NewDocument();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var first = table.Rows.Add().Cells[0].Blocks.AddParagraph("A");
        first.SpacingAfter = Unit.FromPoint(spacingAfter);
        table.Rows.Add().Cells[0].Blocks.AddParagraph("B");
        return builder;
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
        static DocumentBuilder Author(double spacingBefore)
        {
            var builder = TwoParagraphCell(spacingBefore);
            builder.Sections[0].Blocks.AddParagraph("After");
            return builder;
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
        var (builder, section) = NewDocument();
        var table = OneCellTable(section, "RightMe");
        table.Columns[0].Alignment = HorizontalAlignment.Left;
        table.Rows[0].Cells[0].Alignment = HorizontalAlignment.Right;

        var (reference, referenceSection) = NewDocument();
        var referenceTable = OneCellTable(referenceSection, "RightMe");
        referenceTable.Rows[0].Cells[0].Alignment = HorizontalAlignment.Right;

        var expected = Run(reference, "RightMe").X;
        Assert.True(expected > 55, "reference cell-aligned text must sit right of the left content edge");
        Assert.Equal(expected, Run(builder, "RightMe").X, Tol);
    }

    [Fact]
    public void NamedStyleAlignment_BeatsAlignmentInheritedFromRow()
    {
        var (builder, section) = NewDocument();
        builder.Styles.Add("Numeric").Alignment = HorizontalAlignment.Right;
        var table = OneCellTable(section, "RightMe");
        table.Rows[0].Alignment = HorizontalAlignment.Center;
        ((Paragraph)table.Rows[0].Cells[0].Blocks[0]).StyleName = "Numeric";

        var (reference, referenceSection) = NewDocument();
        var referenceTable = OneCellTable(referenceSection, "RightMe");
        ((Paragraph)referenceTable.Rows[0].Cells[0].Blocks[0]).Alignment = HorizontalAlignment.Right;

        var expected = Run(reference, "RightMe").X;
        Assert.True(expected > 55, "reference right-aligned text must sit right of the left content edge");
        Assert.Equal(expected, Run(builder, "RightMe").X, Tol);
    }

    [Fact]
    public void NamedStyleAlignment_BeatsAlignmentInheritedFromColumn()
    {
        var (builder, section) = NewDocument();
        builder.Styles.Add("Middle").Alignment = HorizontalAlignment.Center;
        var table = OneCellTable(section, "MidMe");
        table.Columns[0].Alignment = HorizontalAlignment.Left;
        ((Paragraph)table.Rows[0].Cells[0].Blocks[0]).StyleName = "Middle";

        var (reference, referenceSection) = NewDocument();
        var referenceTable = OneCellTable(referenceSection, "MidMe");
        ((Paragraph)referenceTable.Rows[0].Cells[0].Blocks[0]).Alignment = HorizontalAlignment.Center;

        var expected = Run(reference, "MidMe").X;
        Assert.True(expected > 55, "reference centered text must sit right of the left content edge");
        Assert.Equal(expected, Run(builder, "MidMe").X, Tol);
    }


    private static void AssertIndentedEdges(DocumentBuilder builder)
    {
        var vertical = VerticalSegments(builder);
        Assert.True(vertical.Count > 0, "table borders with Width > 0 must draw vertical edges");

        var minX = vertical.Min(s => s.X1);
        var maxX = vertical.Max(s => s.X1);

        Assert.Equal(150, minX, Tol);
        Assert.True(maxX <= 350 + Tol, $"indented auto table must not cross the right content edge (right edge at {maxX})");
    }

    [Fact]
    public void IndentedAutoWidthTable_InBody_StaysInsideRightContentEdge()
    {
        var (builder, section) = NewDocument();
        var table = OneCellTable(section, "X");
        table.Columns[0].Width = null;
        table.LeftIndent = Unit.FromPoint(100);
        table.Borders.Width = 1;

        AssertIndentedEdges(builder);
    }

    [Fact]
    public void IndentedAutoWidthTable_InHeaderBand_StaysInsideRightContentEdge()
    {
        var (builder, section) = NewDocument();
        section.Blocks.AddParagraph("Body");

        var table = section.Header.Blocks.AddTable();
        table.Columns.Add();
        table.Rows.Add().Cells[0].Blocks.AddParagraph("H");
        table.LeftIndent = Unit.FromPoint(100);
        table.Borders.Width = 1;

        AssertIndentedEdges(builder);
    }

    [Fact]
    public void IndentedAutoWidthNestedTable_StaysInsideOuterCell()
    {
        var (builder, section) = NewDocument();
        var outer = section.Blocks.AddTable();
        outer.Columns.Add(Unit.FromPoint(200));
        var cell = outer.Rows.Add().Cells[0];

        var nested = cell.Blocks.AddTable();
        nested.Columns.Add();
        nested.Rows.Add().Cells[0].Blocks.AddParagraph("N");
        nested.LeftIndent = Unit.FromPoint(80);
        nested.Borders.Width = 1;

        var vertical = VerticalSegments(builder);
        Assert.True(vertical.Count > 0, "nested table borders must draw vertical edges");

        var minX = vertical.Min(s => s.X1);
        var maxX = vertical.Max(s => s.X1);

        Assert.Equal(130, minX, Tol);
        Assert.True(maxX <= 250 + Tol, $"indented nested table must not cross the outer cell right edge (right edge at {maxX})");
    }
}
