#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A CornerRadius > 0 Cell/Container clips its CHILD content (fills, edges, images, texts)
// to the rounded rectangle, so child cell backgrounds no longer poke past the rounded
// corners. Table.CornerRadius rounds a whole table without a wrapper Container: one
// rounded border around the fragment perimeter and the table's draws clipped to it.
// CornerRadius == 0 everywhere stays byte-identical to a document that never set it.
public class RoundedClipTests
{
    private static DocumentBuilder RoundedContainerBuilder(double radius, Action<Container>? configure = null)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(230, 230, 230),
            CornerRadius = Unit.FromPoint(radius),
        });
        configure?.Invoke(container);
        var child = container.Blocks.AddTable();
        child.Columns.Add(Unit.FromPoint(150));
        var cell = child.Rows.Add().Cells[0];
        cell.Background = Color.FromRgb(200, 60, 60);
        var run = cell.Blocks.AddParagraph().Inlines.Add("Child");
        run.Font.Name = BuildTestSupport.Latin;
        return builder;
    }

    private static DocumentBuilder RoundedTableBuilder(double radius, double? borderWidth = 1)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(150));
        table.Columns.Add(Unit.FromPoint(150));
        if (borderWidth is { } width)
        {
            table.Borders.Width = width;
        }

        if (radius > 0)
        {
            table.CornerRadius = Unit.FromPoint(radius);
        }

        for (var r = 0; r < 2; r++)
        {
            var row = table.Rows.Add();
            for (var c = 0; c < 2; c++)
            {
                var cell = row.Cells[c];
                cell.Background = Color.FromRgb(200, 220, 255);
                var run = cell.Blocks.AddParagraph().Inlines.Add($"R{r}C{c}");
                run.Font.Name = BuildTestSupport.Latin;
            }
        }

        return builder;
    }

    private static string FirstPageContent(DocumentBuilder builder)
    {
        var document = builder.Build();
        var page = Assert.Single(document.Pages);
        return Encoding.ASCII.GetString(page.GetContent()!);
    }

    private static int Count(string content, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = content.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    // The Bezier segments of the rounded path that a marker ("h\nW n\n" for a clip,
    // "h\nS\n" for a stroke, "h\nf\n" for a fill) terminates - four "x1 y1 x2 y2 x3 y3 c"
    // lines that fully determine the path's bounds and radius.
    private static List<string> PathCurves(string content, string marker, int occurrence = 0)
    {
        var index = -1;
        for (var i = 0; i <= occurrence; i++)
        {
            index = content.IndexOf(marker, index + 1, StringComparison.Ordinal);
            Assert.True(index >= 0, $"occurrence {occurrence} of '{marker.Replace("\n", "\\n")}' not found");
        }

        var lines = content[..index].Split('\n');
        var curves = new List<string>();
        for (var i = lines.Length - 1; i >= 0 && curves.Count < 4; i--)
        {
            if (lines[i].EndsWith(" c", StringComparison.Ordinal))
            {
                curves.Add(lines[i]);
            }
            else if (lines[i].EndsWith(" rg", StringComparison.Ordinal) || lines[i] == "q")
            {
                break;
            }
        }

        Assert.Equal(4, curves.Count);
        curves.Reverse();
        return curves;
    }

    [Fact]
    public void RoundedContainer_ClipsChildCellBackground_WithRoundedClipPath()
    {
        var content = FirstPageContent(RoundedContainerBuilder(8));

        // The child cell's square background (re f) sits inside a q .. Q group whose clip
        // is a rounded path: four Beziers closed with h, set as clip with W n.
        Assert.Contains("h\nW n\n", content);
        var clip = content.IndexOf("h\nW n\n", StringComparison.Ordinal);
        var groupEnd = content.IndexOf("Q\n", clip, StringComparison.Ordinal);
        Assert.True(groupEnd > clip);
        Assert.Contains("re f\n", content[clip..groupEnd]);
    }

    [Fact]
    public void RoundedContainer_ClipUsesTheContainerBoundsAndRadius()
    {
        var content = FirstPageContent(RoundedContainerBuilder(8));

        // The container's own rounded background fill and the child clip trace the same
        // rounded rectangle, so Clip == container bounds and ClipRadius == the clamped radius.
        Assert.Equal(PathCurves(content, "h\nf\n"), PathCurves(content, "h\nW n\n"));
    }

    [Fact]
    public void RoundedContainer_DoesNotClipItsOwnBorderStroke()
    {
        var builder = RoundedContainerBuilder(8, container => container.Borders.Width = 1);

        var content = FirstPageContent(builder);

        var stroke = content.IndexOf("h\nS\n", StringComparison.Ordinal);
        Assert.True(stroke >= 0, "rounded border stroke missing");
        // The stroke group (q .. h S Q) contains no clip of its own.
        var groupStart = content.LastIndexOf("q\n", stroke, StringComparison.Ordinal);
        Assert.DoesNotContain("W n", content[groupStart..stroke]);
    }

    [Fact]
    public void TableCornerRadius_EmitsARoundedOuterBorder()
    {
        var content = FirstPageContent(RoundedTableBuilder(8));

        Assert.Equal(1, Count(content, "h\nS\n"));
    }

    [Fact]
    public void TableCornerRadius_ClipsCornerCellBackgroundsToTheTableShape()
    {
        var content = FirstPageContent(RoundedTableBuilder(8));

        // All four square cell backgrounds carry the rounded clip.
        Assert.Equal(4, Count(content, "re f\n"));
        Assert.True(Count(content, "h\nW n\n") >= 4);
        var frame = PathCurves(content, "h\nS\n");
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(frame, PathCurves(content, "h\nW n\n", i));
        }
    }

    [Fact]
    public void TableCornerRadius_ClipsTheOuterBorderEdges()
    {
        var content = FirstPageContent(RoundedTableBuilder(8));

        // Cell edge strokes (x1 y1 m x2 y2 l S) are clipped to the rounded table box too.
        var edge = content.IndexOf(" l\nS\nQ\n", StringComparison.Ordinal);
        Assert.True(edge >= 0, "no edge stroke found");
        var groupStart = content.LastIndexOf("q\n", edge, StringComparison.Ordinal);
        Assert.Contains("W n", content[groupStart..edge]);
    }

    [Fact]
    public void TableCornerRadius_IsClampedToHalfTheSmallerDimension()
    {
        var content = FirstPageContent(RoundedTableBuilder(500));

        var curves = PathCurves(content, "h\nS\n");
        static double Num(string line, int index)
            => double.Parse(line.Split(' ')[index], CultureInfo.InvariantCulture);

        // First curve ends at (right, bottom + r); its control y1 sits on the bottom edge.
        var bottom = Num(curves[0], 1);
        var radius = Num(curves[0], 5) - bottom;
        // Third curve ends at (left, top - r), so top - bottom is the fragment height.
        var height = Num(curves[2], 5) + radius - bottom;
        Assert.True(height is > 0 and < 300, $"unexpected table height {height}");
        Assert.Equal(height / 2, radius, 2);
    }

    [Fact]
    public void CornerRadiusZero_Everywhere_IsByteIdenticalToUntouched()
    {
        static byte[] Build(bool setZero)
        {
            var builder = new DocumentBuilder();
            BuildTestSupport.RegisterLatin(builder);
            var section = builder.Sections.Add();
            var container = section.Blocks.Add(new Container
            {
                Padding = Unit.FromPoint(10),
                Background = Color.FromRgb(230, 230, 230),
            });
            container.Borders.Width = 1;
            var table = container.Blocks.AddTable();
            table.Columns.Add(Unit.FromPoint(120));
            table.Columns.Add(Unit.FromPoint(120));
            table.Borders.Width = 1;
            var row = table.Rows.Add();
            row.Cells[0].Background = Color.FromRgb(200, 220, 255);
            var run = row.Cells[0].Blocks.AddParagraph().Inlines.Add("Zero");
            run.Font.Name = BuildTestSupport.Latin;
            if (setZero)
            {
                container.CornerRadius = Unit.FromPoint(0);
                table.CornerRadius = Unit.FromPoint(0);
                row.Cells[0].CornerRadius = Unit.FromPoint(0);
            }

            return builder.ToArray();
        }

        Assert.Equal(Build(setZero: false), Build(setZero: true));
    }
}
