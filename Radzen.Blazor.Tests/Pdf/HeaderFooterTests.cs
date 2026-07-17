#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class HeaderFooterTests
{
    private const double Tol = 1e-6;

    private static Section BuildMultiPage(FontCollection fonts, out Paragraph header, out Paragraph footer)
    {
        var lineH = PaginationSupport.LineHeight(fonts);
        var contentH = PaginationSupport.HeightForLines(lineH, 3);
        var section = PaginationSupport.Section(500, contentH + 40 + 50);
        section.Margins.Left = Unit.FromPoint(30);
        section.Margins.Right = Unit.FromPoint(30);
        section.Margins.Top = Unit.FromPoint(40);
        section.Margins.Bottom = Unit.FromPoint(50);

        header = section.Header.Blocks.Add(PaginationSupport.Text("HEADER"));
        footer = section.Footer.Blocks.Add(PaginationSupport.Text("FOOTER"));

        for (var i = 0; i < 8; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"B{i}"));
        }

        return section;
    }

    [Fact]
    public void EightLines_ThreePerContentPage_ProduceThreePages()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out _, out _);

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(3, pages.Count);
        Assert.Equal(3, pages[0].Lines.Count);
        Assert.Equal(3, pages[1].Lines.Count);
        Assert.Equal(2, pages[2].Lines.Count);
    }

    [Fact]
    public void Header_AppearsOnEveryPage_WithSameSource()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out var header, out _);

        var pages = Paginator.Paginate(section, fonts);

        Assert.All(pages, p => Assert.NotEmpty(p.Header));
        Assert.All(pages, p => Assert.Same(header, p.Header[0].Source));
    }

    [Fact]
    public void Footer_AppearsOnEveryPage_WithSameSource()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out _, out var footer);

        var pages = Paginator.Paginate(section, fonts);

        Assert.All(pages, p => Assert.NotEmpty(p.Footer));
        Assert.All(pages, p => Assert.Same(footer, p.Footer[0].Source));
    }

    [Fact]
    public void HeaderBand_FlowsFromPageTop()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out _, out _);

        var page = Paginator.Paginate(section, fonts)[0];

        Assert.Equal(0.0, page.Header[0].Y, Tol);
    }

    [Fact]
    public void FooterBand_FlowsFromBottomMarginTop()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out _, out _);

        var page = Paginator.Paginate(section, fonts)[0];

        Assert.Equal(0.0, page.Footer[0].Y, Tol);
    }

    [Fact]
    public void ContentBox_SitsBetweenTheMargins_OnEveryPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var contentH = PaginationSupport.HeightForLines(lineH, 3);
        var section = BuildMultiPage(fonts, out _, out _);

        var pages = Paginator.Paginate(section, fonts);

        Assert.All(pages, p =>
        {
            Assert.Equal(30.0, p.ContentBox.X, Tol);
            Assert.Equal(40.0, p.ContentBox.Y, Tol);
            Assert.Equal(440.0, p.ContentBox.Width, Tol);
            Assert.Equal(contentH, p.ContentBox.Height, Tol);
        });
    }

    [Fact]
    public void BodyLines_StayWithinTheContentHeight()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = BuildMultiPage(fonts, out _, out _);

        var pages = Paginator.Paginate(section, fonts);

        Assert.All(pages, p => Assert.All(p.Lines,
            l => Assert.True(l.Y + l.Line.Height <= p.ContentBox.Height + Tol)));
    }

    [Fact]
    public void NoHeaderOrFooter_LeavesBandsEmpty()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(500, 400);
        section.Blocks.Add(PaginationSupport.Text("solo"));

        var page = Paginator.Paginate(section, fonts)[0];

        Assert.Empty(page.Header);
        Assert.Empty(page.Footer);
    }
}
