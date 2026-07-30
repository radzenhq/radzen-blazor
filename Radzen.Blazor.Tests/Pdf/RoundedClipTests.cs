#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class RoundedClipTests
{
    private static Document RoundedContainerBuilder(double radius, Action<Container>? configure = null)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
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
        run.Font.Family = BuildTestSupport.Latin;
        return document;
    }

    private static Document RoundedTableBuilder(double radius, double? borderWidth = 1)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
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
                run.Font.Family = BuildTestSupport.Latin;
            }
        }

        return document;
    }

    private static string FirstPageContent(Document document)
    {
        var pdf = new DocumentRenderer().Render(document);
        var page = Assert.Single(pdf.Pages);
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

        Assert.Equal(PathCurves(content, "h\nf\n"), PathCurves(content, "h\nW n\n"));
    }

    [Fact]
    public void RoundedContainer_DoesNotClipItsOwnBorderStroke()
    {
        var document = RoundedContainerBuilder(8, container => container.Borders.Width = 1);

        var content = FirstPageContent(document);

        var stroke = content.IndexOf("h\nS\n", StringComparison.Ordinal);
        Assert.True(stroke >= 0, "rounded border stroke missing");
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

        var bottom = Num(curves[0], 1);
        var radius = Num(curves[0], 5) - bottom;
        var height = Num(curves[2], 5) + radius - bottom;
        Assert.True(height is > 0 and < 300, $"unexpected table height {height}");
        Assert.Equal(height / 2, radius, 2);
    }

    [Fact]
    public void CornerRadiusZero_Everywhere_IsByteIdenticalToUntouched()
    {
        static byte[] Build(bool setZero)
        {
            var document = new Document();
            BuildTestSupport.RegisterLatin(document);
            var section = document.Sections.Add();
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
            run.Font.Family = BuildTestSupport.Latin;
            if (setZero)
            {
                container.CornerRadius = Unit.FromPoint(0);
                table.CornerRadius = Unit.FromPoint(0);
            }

            return new DocumentRenderer().ToArray(document);
        }

        Assert.Equal(Build(setZero: false), Build(setZero: true));
    }
}
