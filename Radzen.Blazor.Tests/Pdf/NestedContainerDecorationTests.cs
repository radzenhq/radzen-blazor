#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A nested table (inside a cell) and a nested container (inside a cell or box) must paint
// their decoration exactly like their top-level counterparts: nested table rows keep their
// backgrounds, and nested containers keep gradient backgrounds and the graphics-state
// options (blend/overprint) a top-level box honours.
public class NestedContainerDecorationTests
{
    private static List<ContentOperation> Ops(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
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

    // A row background on a table nested inside a cell paints a filled rectangle, just as a
    // top-level table row does. Green is 0, 0.502, 0 in rg space and no other draw uses it.
    [Fact]
    public void NestedTable_PaintsRowBackground()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        var outer = section.Blocks.AddTable();
        outer.Columns.Add(Unit.FromPoint(300));
        var host = outer.Rows.Add().Cells[0];
        TableLayoutSupport.Fill(host, "OUTER");

        var inner = host.Blocks.AddTable();
        inner.Columns.Add(Unit.FromPoint(120));
        var innerRow = inner.Rows.Add();
        innerRow.Background = Color.Green;
        TableLayoutSupport.Fill(innerRow.Cells[0], "NESTED");

        var ops = Ops(builder);
        Assert.True(HasColorOperation(ops, "rg", 0, 0.502, 0),
            "nested table row background emits a fill in the row background color");
    }

    // A gradient background on a container nested inside a cell realizes as a shading pattern
    // (/Pattern cs + scn), the same as a top-level container. A dropped gradient would fall
    // back to a solid fill or nothing.
    [Fact]
    public void NestedContainer_PaintsGradientBackground()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
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
        boxed.Font.Name = BuildTestSupport.Latin;

        var ops = Ops(builder);
        Assert.True(HasOperator(ops, "scn"),
            "nested container gradient background selects a shading pattern via scn");
    }
}
