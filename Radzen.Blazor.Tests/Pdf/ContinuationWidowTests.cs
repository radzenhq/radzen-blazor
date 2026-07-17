#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

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

        var pages = Paginator.Paginate(section, fonts);

        Assert.True(pages.Count >= 3, $"expected the paragraph to span 3+ pages, got {pages.Count}");
        Assert.True(
            pages[^1].Lines.Count >= para.Widows,
            $"final page has {pages[^1].Lines.Count} lines, fewer than Widows={para.Widows}");
        Assert.Equal(11, pages.Sum(p => p.Lines.Count));
    }
}
