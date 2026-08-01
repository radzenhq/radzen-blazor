#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class PaginatorLayoutEdgeCaseTests
{
    [Fact]
    public void HugeSpacingBefore_ClampsSoFirstLineStaysOnPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight();
        var section = PaginationSupport.Section(200, PaginationSupport.HeightForLines(lineH, 5));
        var para = PaginationSupport.Text("Alpha");
        para.SpacingBefore = Unit.FromPoint(1000);
        section.Blocks.Add(para);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        var page = Assert.Single(pages);
        var line = Assert.Single(page.Body.Lines);
        Assert.True(line.Y + line.Line.Height <= page.ContentBox.Height + 1e-6,
            $"first line at {line.Y} overflows content height {page.ContentBox.Height}");
        Assert.Equal("Alpha", string.Concat(line.Line.Fragments.Select(f => f.Text)));
    }

    [Fact]
    public void ZeroOrphans_DoesNotEmitBlankFirstPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight();
        var width = PaginationSupport.WidthForWordsPerLine(fonts, "Ha", 2, 12);
        var section = PaginationSupport.Section(width, PaginationSupport.HeightForLines(lineH, 1));
        var para = PaginationSupport.Repeated("Ha", 4);
        para.Orphans = 0;
        para.Widows = 2;
        section.Blocks.Add(para);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.NotEmpty(pages[0].Body.Lines);
        Assert.Equal(2, pages.Sum(p => p.Body.Lines.Length));
    }
}
