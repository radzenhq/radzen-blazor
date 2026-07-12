#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// CornerRadius on Container/Cell rounds the box: the background fills a genuine
// rounded-rectangle path (four cubic Beziers), and a uniform border strokes the same
// path with a single S. Non-uniform borders keep the square four-edge rendering.
// CornerRadius == 0 must be byte-identical to a document that never set it.
public class RoundedCornersTests
{
    private static DocumentBuilder Builder(Action<Container>? configure = null)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(230, 230, 230),
        });
        var paragraph = container.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("Panel");
        run.Font.Name = BuildTestSupport.Latin;
        configure?.Invoke(container);
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

    [Fact]
    public void RoundedFill_WithUniformBorder_EmitsTwoBezierPaths()
    {
        var builder = Builder(container =>
        {
            container.CornerRadius = Unit.FromPoint(6);
            container.Borders.Width = 1;
        });

        var content = FirstPageContent(builder);

        // Fill path + border path + the rounded clip around the child paragraph text.
        Assert.Equal(12, Count(content, " c\n"));
        Assert.Equal(1, Count(content, "h\nf\n"));
        Assert.Equal(1, Count(content, "h\nS\n"));
        Assert.DoesNotContain("re f", content);
    }

    [Fact]
    public void RoundedFill_PaintsUnderTheRoundedBorder()
    {
        var builder = Builder(container =>
        {
            container.CornerRadius = Unit.FromPoint(6);
            container.Borders.Width = 1;
        });

        var content = FirstPageContent(builder);

        var fill = content.IndexOf("h\nf\n", StringComparison.Ordinal);
        var stroke = content.IndexOf("h\nS\n", StringComparison.Ordinal);
        Assert.True(fill >= 0 && stroke >= 0);
        Assert.True(fill < stroke, "rounded fill must precede the rounded border stroke");
    }

    [Fact]
    public void RoundedFill_WithoutBorder_EmitsOnlyTheFillPath()
    {
        var builder = Builder(container => container.CornerRadius = Unit.FromPoint(6));

        var content = FirstPageContent(builder);

        // Fill path + the rounded clip around the child paragraph text.
        Assert.Equal(8, Count(content, " c\n"));
        Assert.Equal(1, Count(content, "h\nf\n"));
        Assert.Equal(0, Count(content, "h\nS\n"));
    }

    [Fact]
    public void NonUniformBorder_KeepsSquareEdges_AndRoundsOnlyTheFill()
    {
        var builder = Builder(container =>
        {
            container.CornerRadius = Unit.FromPoint(6);
            container.Borders.Width = 1;
            container.Borders.Left.Width = 3;
        });

        var content = FirstPageContent(builder);

        // Fill path + the rounded clip around the child paragraph text.
        Assert.Equal(8, Count(content, " c\n"));
        Assert.Equal(1, Count(content, "h\nf\n"));
        Assert.Equal(0, Count(content, "h\nS\n"));
        Assert.Equal(4, Count(content, " l\nS\nQ\n"));
    }

    [Fact]
    public void CornerRadius_IsClampedToHalfTheSmallerBoxDimension()
    {
        var builder = Builder(container =>
        {
            container.Width = Unit.FromPoint(120);
            container.CornerRadius = Unit.FromPoint(500);
        });

        var content = FirstPageContent(builder);
        var (radius, width, height) = ParseRoundedFill(content);

        Assert.True(width > 0 && height > 0);
        Assert.Equal(Math.Min(width, height) / 2, radius, 2);
    }

    [Fact]
    public void CornerRadiusZero_IsByteIdenticalToUntouched()
    {
        var untouched = Builder(container => container.Borders.Width = 1).ToArray();
        var zeroed = Builder(container =>
        {
            container.Borders.Width = 1;
            container.CornerRadius = Unit.FromPoint(0);
        }).ToArray();

        Assert.Equal(untouched, zeroed);
    }

    [Fact]
    public void TableCell_CornerRadius_RoundsTheCellBackground()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var table = section.Blocks.Add(new Table());
        table.Columns.Add(Unit.FromPoint(150));
        var cell = table.Rows.Add().Cells[0];
        cell.Background = Color.FromRgb(200, 220, 255);
        cell.CornerRadius = Unit.FromPoint(5);
        cell.Padding = Unit.FromPoint(6);
        var run = cell.Blocks.AddParagraph().Inlines.Add("Rounded");
        run.Font.Name = BuildTestSupport.Latin;

        var content = FirstPageContent(builder);

        // Fill path + the rounded clip around the cell's paragraph text.
        Assert.Equal(8, Count(content, " c\n"));
        Assert.Equal(1, Count(content, "h\nf\n"));
    }

    // Reconstructs (radius, width, height) from the emitted rounded fill path, whose
    // first segment is: (x+r) y m, (x+w-r) y l, ... c ending at (x+w) (y+r), then
    // l to (x+w) (y+h-r), c ..., l to (x+r) (y+h).
    private static (double Radius, double Width, double Height) ParseRoundedFill(string content)
    {
        var closeFill = content.IndexOf("h\nf\n", StringComparison.Ordinal);
        Assert.True(closeFill >= 0, "no rounded fill path found");
        var start = content.LastIndexOf("rg\n", closeFill, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var lines = content[(start + 3)..closeFill]
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' '))
            .ToList();

        static double Num(string token) => double.Parse(token, CultureInfo.InvariantCulture);

        Assert.Equal("m", lines[0][^1]);
        Assert.Equal("c", lines[2][^1]);
        Assert.Equal("l", lines[3][^1]);
        Assert.Equal("l", lines[5][^1]);
        var y = Num(lines[0][1]);
        var firstCurveEndX = Num(lines[2][4]);
        var firstCurveEndY = Num(lines[2][5]);
        var radius = firstCurveEndY - y;
        var x = Num(lines[0][0]) - radius;
        var width = firstCurveEndX - x;
        var top = Num(lines[5][1]);
        var height = top - y;
        return (radius, width, height);
    }
}
