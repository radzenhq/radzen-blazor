#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class ContinuationWidowTests
{
    private static double Width(FontCollection fonts)
        => PaginationSupport.WidthForWordsPerLine(fonts, "Ha", 2, 12);

    [Fact]
    public void ParagraphAcrossThreePages_KeepsWidowsLinesOnFinalPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lh = PaginationSupport.LineHeight(fonts);

        var section = PaginationSupport.Section(Width(fonts), PaginationSupport.HeightForLines(lh, 5));
        var para = PaginationSupport.Repeated("Ha", 22);
        section.Blocks.Add(para);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.True(pages.Length >= 3, $"expected the paragraph to span 3+ pages, got {pages.Length}");
        Assert.True(
            pages[^1].Body.Lines.Length >= para.Widows,
            $"final page has {pages[^1].Body.Lines.Length} lines, fewer than Widows={para.Widows}");
        Assert.Equal(11, pages.Sum(p => p.Body.Lines.Length));
    }
}
