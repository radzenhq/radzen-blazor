#nullable enable
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// KeepTogether: a paragraph that does not fit whole in the remaining space moves as a
// unit to the next page instead of splitting. KeepWithNext: a heading is pulled to the
// next page when its following block's first line would not fit beneath it.
public class KeepTogetherTests
{
    private const double Tol = 1e-6;

    // Page fits exactly 5 lines; two single-line fillers then a 4-line paragraph, so the
    // paragraph meets a page boundary with only 3 of its 4 lines fitting.
    private static (Section Section, Paragraph Para) BuildBoundary(FontCollection fonts, bool keepTogether)
    {
        var lineH = PaginationSupport.LineHeight(fonts);
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

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(2, pages.Count);
        Assert.Equal(4, pages[0].Lines.Count);
        Assert.Equal(2, pages[1].Lines.Count);
    }

    [Fact]
    public void KeepTogether_MovesTheWholeParagraphToTheNextPage()
    {
        var fonts = PaginationSupport.Fonts();
        var (section, para) = BuildBoundary(fonts, keepTogether: true);

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(2, pages.Count);
        Assert.Equal(2, pages[0].Lines.Count);
        Assert.Equal(4, pages[1].Lines.Count);
        Assert.Same(para, pages[1].Lines[0].Source);
    }

    [Fact]
    public void KeepTogether_MovedParagraphStartsAtContentTop()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var (section, _) = BuildBoundary(fonts, keepTogether: true);

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(0.0, pages[1].Lines[0].Y, Tol);
        Assert.Equal(lineH, pages[1].Lines[1].Y, Tol);
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

        var pages = Paginator.Paginate(section, fonts);

        Assert.Single(pages);
        Assert.Equal(5, pages[0].Lines.Count);
    }

    // Heading lands as the last fitting line at the bottom; KeepWithNext must pull it
    // down so it shares a page with the first line of the body that follows.
    [Fact]
    public void KeepWithNext_PullsAHeadingToTheNextPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 3));

        section.Blocks.Add(PaginationSupport.Text("t0"));
        section.Blocks.Add(PaginationSupport.Text("t1"));
        var heading = PaginationSupport.Text("Heading");
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);
        var body = section.Blocks.Add(PaginationSupport.Text("body"));

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(2, pages.Count);
        Assert.Equal(2, pages[0].Lines.Count);
        Assert.Same(heading, pages[1].Lines[0].Source);
        Assert.Same(body, pages[1].Lines[1].Source);
    }

    [Fact]
    public void WithoutKeepWithNext_TheHeadingStaysAtTheBottom()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 3));

        section.Blocks.Add(PaginationSupport.Text("t0"));
        section.Blocks.Add(PaginationSupport.Text("t1"));
        var heading = section.Blocks.Add(PaginationSupport.Text("Heading"));
        var body = section.Blocks.Add(PaginationSupport.Text("body"));

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(2, pages.Count);
        Assert.Equal(3, pages[0].Lines.Count);
        Assert.Same(heading, pages[0].Lines[2].Source);
        Assert.Same(body, pages[1].Lines[0].Source);
    }
}
