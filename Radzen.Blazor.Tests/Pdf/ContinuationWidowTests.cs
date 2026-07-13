#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Widow control must hold at EVERY page break of a paragraph, not only the first. A
// paragraph spanning 3+ pages must never strand fewer than Widows lines on its final page.
public class ContinuationWidowTests
{
    private static double Width(FontCollection fonts)
        => PaginationSupport.WidthForWordsPerLine(fonts, "Ha", 2, 12);

    [Fact]
    public void ParagraphAcrossThreePages_KeepsWidowsLinesOnFinalPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lh = PaginationSupport.LineHeight(fonts);

        // 11 single lines on a 5-line page: page 1 takes 5, and the continuation break must
        // leave >= Widows lines for the last page instead of a lone widow.
        var section = PaginationSupport.Section(Width(fonts), PaginationSupport.HeightForLines(lh, 5));
        var para = PaginationSupport.Repeated("Ha", 22);
        section.Blocks.Add(para);

        var pages = Paginator.Paginate(section, fonts);

        Assert.True(pages.Count >= 3, $"expected the paragraph to span 3+ pages, got {pages.Count}");
        Assert.True(
            pages[^1].Lines.Count >= para.Widows,
            $"final page has {pages[^1].Lines.Count} lines, fewer than Widows={para.Widows}");
        Assert.Equal(11, pages.Sum(p => p.Lines.Count));
    }
}
