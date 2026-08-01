#nullable enable
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class KeepTogetherTests
{
    private const double Tol = 1e-6;

    private static (Section Section, Paragraph Para) BuildBoundary(FontCollection fonts, bool keepTogether)
    {
        var lineH = PaginationSupport.LineHeight();
        var width = PaginationSupport.WidthForWordsPerLine(fonts, "Ha", 2, 12);
        var section = PaginationSupport.Section(width, PaginationSupport.HeightForLines(lineH, 5));

        section.Blocks.Add(PaginationSupport.Text("f0"));
        section.Blocks.Add(PaginationSupport.Text("f1"));
        var para = PaginationSupport.Repeated("Ha", 8);
        para.KeepTogether = keepTogether;
        section.Blocks.Add(para);
        return (section, para);
    }

    [Fact]
    public void WithoutKeepTogether_TheParagraphSplitsAcrossTheBoundary()
    {
        var fonts = PaginationSupport.Fonts();
        var (section, _) = BuildBoundary(fonts, keepTogether: false);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(2, pages.Length);
        Assert.Equal(4, pages[0].Body.Lines.Length);
        Assert.Equal(2, pages[1].Body.Lines.Length);
    }

    [Fact]
    public void KeepTogether_MovesTheWholeParagraphToTheNextPage()
    {
        var fonts = PaginationSupport.Fonts();
        var (section, para) = BuildBoundary(fonts, keepTogether: true);

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Equal(2, pages.Length);
        Assert.Equal(2, pages[0].Body.Lines.Length);
        Assert.Equal(4, pages[1].Body.Lines.Length);
        Assert.Equal(capture.Source(para), pages[1].Body.Lines[0].Source);
    }

    [Fact]
    public void KeepTogether_MovedParagraphStartsAtContentTop()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight();
        var (section, _) = BuildBoundary(fonts, keepTogether: true);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(0.0, pages[1].Body.Lines[0].Y, Tol);
        Assert.Equal(lineH, pages[1].Body.Lines[1].Y, Tol);
    }

    [Fact]
    public void KeepTogether_LeavesTheParagraphInPlaceWhenItFits()
    {
        var fonts = PaginationSupport.Fonts();
        var width = PaginationSupport.WidthForWordsPerLine(fonts, "Ha", 2, 12);
        var section = PaginationSupport.Section(width, 400);
        section.Blocks.Add(PaginationSupport.Text("f0"));
        var para = PaginationSupport.Repeated("Ha", 8);
        para.KeepTogether = true;
        section.Blocks.Add(para);

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Single(pages);
        Assert.Equal(5, pages[0].Body.Lines.Length);
    }

    [Fact]
    public void KeepWithNext_PullsAHeadingToTheNextPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight();
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 3));

        section.Blocks.Add(PaginationSupport.Text("t0"));
        section.Blocks.Add(PaginationSupport.Text("t1"));
        var heading = PaginationSupport.Text("Heading");
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);
        var body = section.Blocks.Add(PaginationSupport.Text("body"));

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Equal(2, pages.Length);
        Assert.Equal(2, pages[0].Body.Lines.Length);
        Assert.Equal(capture.Source(heading), pages[1].Body.Lines[0].Source);
        Assert.Equal(capture.Source(body), pages[1].Body.Lines[1].Source);
    }

    [Fact]
    public void WithoutKeepWithNext_TheHeadingStaysAtTheBottom()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight();
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 3));

        section.Blocks.Add(PaginationSupport.Text("t0"));
        section.Blocks.Add(PaginationSupport.Text("t1"));
        var heading = section.Blocks.Add(PaginationSupport.Text("Heading"));
        var body = section.Blocks.Add(PaginationSupport.Text("body"));

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.Equal(2, pages.Length);
        Assert.Equal(3, pages[0].Body.Lines.Length);
        Assert.Equal(capture.Source(heading), pages[0].Body.Lines[2].Source);
        Assert.Equal(capture.Source(body), pages[1].Body.Lines[0].Source);
    }
}
