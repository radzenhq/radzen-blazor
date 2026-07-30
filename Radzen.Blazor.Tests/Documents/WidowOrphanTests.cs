#nullable enable
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class WidowOrphanTests
{
    private const double Tol = 1e-6;

    private static double Width(FontCollection fonts)
        => PaginationSupport.WidthForWordsPerLine(fonts, "Ha", 2, 12);

    private static (Section, Paragraph) WidowScenario(FontCollection fonts, int widows)
    {
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(Width(fonts), PaginationSupport.HeightForLines(lineH, 5));
        section.Blocks.Add(PaginationSupport.Text("f0"));
        section.Blocks.Add(PaginationSupport.Text("f1"));
        var para = PaginationSupport.Repeated("Ha", 8);
        para.Widows = widows;
        section.Blocks.Add(para);
        return (section, para);
    }

    [Fact]
    public void DefaultWidows_PullsALineDownToAvoidASingleWidow()
    {
        var fonts = PaginationSupport.Fonts();
        var (section, _) = WidowScenario(fonts, widows: 2);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(2, pages.Length);
        Assert.Equal(4, pages[0].Body.Lines.Length);
        Assert.Equal(2, pages[1].Body.Lines.Length);
    }

    [Fact]
    public void DefaultWidows_SplitLinesLandAtTheExpectedOffsets()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var (section, para) = WidowScenario(fonts, widows: 2);

        var capture = new LayoutCaptureContext();
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Equal(capture.Source(para), pages[0].Body.Lines[2].Source);
        Assert.Equal(capture.Source(para), pages[0].Body.Lines[3].Source);
        Assert.Equal(2 * lineH, pages[0].Body.Lines[2].Y, Tol);
        Assert.Equal(3 * lineH, pages[0].Body.Lines[3].Y, Tol);
        Assert.Equal(0.0, pages[1].Body.Lines[0].Y, Tol);
        Assert.Equal(lineH, pages[1].Body.Lines[1].Y, Tol);
    }

    [Fact]
    public void WidowsOne_AllowsTheSingleTrailingLine()
    {
        var fonts = PaginationSupport.Fonts();
        var (section, _) = WidowScenario(fonts, widows: 1);

        var capture = new LayoutCaptureContext();
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Equal(2, pages.Length);
        Assert.Equal(5, pages[0].Body.Lines.Length);
        Assert.Equal(1, pages[1].Body.Lines.Length);
    }

    private static (Section, Paragraph) OrphanScenario(FontCollection fonts, int orphans)
    {
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(Width(fonts), PaginationSupport.HeightForLines(lineH, 5));
        for (var i = 0; i < 4; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"f{i}"));
        }

        var para = PaginationSupport.Repeated("Ha", 6);
        para.Orphans = orphans;
        section.Blocks.Add(para);
        return (section, para);
    }

    [Fact]
    public void DefaultOrphans_PushTheWholeParagraphWhenOnlyOneLineFits()
    {
        var fonts = PaginationSupport.Fonts();
        var (section, para) = OrphanScenario(fonts, orphans: 2);

        var capture = new LayoutCaptureContext();
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Equal(2, pages.Length);
        Assert.Equal(4, pages[0].Body.Lines.Length);
        Assert.Equal(3, pages[1].Body.Lines.Length);
        Assert.Equal(capture.Source(para), pages[1].Body.Lines[0].Source);
        Assert.Equal(0.0, pages[1].Body.Lines[0].Y, Tol);
    }

    [Fact]
    public void OrphansOne_AllowsTheSingleLeadingLineAtTheBottom()
    {
        var fonts = PaginationSupport.Fonts();
        var (section, para) = OrphanScenario(fonts, orphans: 1);

        var capture = new LayoutCaptureContext();
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Equal(2, pages.Length);
        Assert.Equal(5, pages[0].Body.Lines.Length);
        Assert.Equal(2, pages[1].Body.Lines.Length);
        Assert.Equal(capture.Source(para), pages[0].Body.Lines[4].Source);
    }
}
