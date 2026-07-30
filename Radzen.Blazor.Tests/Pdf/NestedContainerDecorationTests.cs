#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class NestedContainerDecorationTests
{
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
                && System.Math.Abs(operation.Num(0) - r) < 0.005
                && System.Math.Abs(operation.Num(1) - g) < 0.005
                && System.Math.Abs(operation.Num(2) - b) < 0.005)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOperator(List<ContentOperation> ops, string op)
    {
        foreach (var operation in ops)
        {
            if (operation.Operator == op)
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void NestedTable_PaintsRowBackground()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var outer = section.Blocks.AddTable();
        outer.Columns.Add(Unit.FromPoint(300));
        var host = outer.Rows.Add().Cells[0];
        TableLayoutSupport.Fill(host, "OUTER");

        var inner = host.Blocks.AddTable();
        inner.Columns.Add(Unit.FromPoint(120));
        var innerRow = inner.Rows.Add();
        innerRow.Background = Color.Green;
        TableLayoutSupport.Fill(innerRow.Cells[0], "NESTED");

        var ops = Ops(document);
        Assert.True(HasColorOperation(ops, "rg", 0, 0.502, 0),
            "nested table row background emits a fill in the row background color");
    }

    [Fact]
    public void NestedContainer_PaintsGradientBackground()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(300));
        var host = table.Rows.Add().Cells[0];
        TableLayoutSupport.Fill(host, "HOST");

        var container = host.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(6),
            Width = Unit.FromPoint(200),
            BackgroundGradient = new LinearGradient(
                0, 0, 100, 0,
                new GradientStop(0, Color.Red),
                new GradientStop(1, Color.Blue)),
        });
        var boxed = container.Blocks.AddParagraph().Inlines.Add("BOXED");
        boxed.Font.Family = BuildTestSupport.Latin;

        var ops = Ops(document);
        Assert.True(HasOperator(ops, "scn"),
            "nested container gradient background selects a shading pattern via scn");
    }

    [Fact]
    public void NestedContainer_PaintsShadow()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(300));
        var nested = table.Rows.Add().Cells[0].Blocks.Add(new Container
        {
            Width = Unit.FromPoint(200),
            Shadow = new BoxShadow
            {
                Color = Color.FromArgb(160, 0, 0, 0),
                BlurRadius = Unit.FromPoint(8),
                OffsetX = Unit.FromPoint(2),
                OffsetY = Unit.FromPoint(3),
            },
        });
        nested.Blocks.AddParagraph("Nested");

        Assert.True(HasOperator(Ops(document), "gs"),
            "nested container shadow emits its transparency graphics state");
    }

    [Fact]
    public void RotatedContainerInsideTableCell_IsRejectedBeforeEmission()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(300));
        var nested = table.Rows.Add().Cells[0].Blocks.Add(new Container { Rotation = 15 });
        nested.Blocks.AddParagraph("Nested");

        var exception = Assert.Throws<System.NotSupportedException>(
            () => new DocumentRenderer().Render(document));

        Assert.Contains("only supported as direct section content", exception.Message);
    }
}
