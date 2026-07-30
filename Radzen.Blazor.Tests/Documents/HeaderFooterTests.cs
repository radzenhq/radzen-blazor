#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class HeaderFooterTests
{
    private const double Tol = 1e-6;

    private static Section BuildMultiPage(FontCollection fonts, out Paragraph header, out Paragraph footer)
    {
        var lineH = PaginationSupport.LineHeight();
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

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(3, pages.Length);
        Assert.Equal(3, pages[0].Body.Lines.Length);
        Assert.Equal(3, pages[1].Body.Lines.Length);
        Assert.Equal(2, pages[2].Body.Lines.Length);
    }

    [Fact]
    public void Header_AppearsOnEveryPage_WithSameSource()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out var header, out _);

        var capture = new LayoutCaptureContext();
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.All(pages, p => Assert.NotEmpty(p.HeaderLayer.Lines));
        Assert.All(pages, p => Assert.Equal(capture.Source(header), p.HeaderLayer.Lines[0].Source));
    }

    [Fact]
    public void Footer_AppearsOnEveryPage_WithSameSource()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out _, out var footer);

        var capture = new LayoutCaptureContext();
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        Assert.All(pages, p => Assert.NotEmpty(p.FooterLayer.Lines));
        Assert.All(pages, p => Assert.Equal(capture.Source(footer), p.FooterLayer.Lines[0].Source));
    }

    [Fact]
    public void HeaderContent_SitsAtTheHeaderDistanceBelowThePageTop()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out _, out _);

        var page = IsolatedPaginator.PaginateIsolated(section, fonts)[0];
        var line = page.HeaderLayer.Lines[0];

        Assert.Equal(section.HeaderDistance.Point, page.HeaderTop + line.Y, Tol);
        Assert.True(page.HeaderTop + line.Y + line.Line.Height <= page.ContentBox.Y + Tol);
    }

    [Fact]
    public void FooterContent_EndsAtTheFooterDistanceAboveThePageBottom()
    {
        var fonts = PaginationSupport.Fonts();
        var section = BuildMultiPage(fonts, out _, out _);

        var page = IsolatedPaginator.PaginateIsolated(section, fonts)[0];
        var line = page.FooterLayer.Lines[0];
        var pageHeight = page.Size.Height.Point;

        Assert.Equal(pageHeight - section.FooterDistance.Point, page.FooterTop + line.Y + line.Line.Height, Tol);
        Assert.True(page.FooterTop + line.Y >= page.ContentBox.Y + page.ContentBox.Height - Tol);
    }

    [Fact]
    public void ContentBox_SitsBetweenTheMargins_OnEveryPage()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight();
        var contentH = PaginationSupport.HeightForLines(lineH, 3);
        var section = BuildMultiPage(fonts, out _, out _);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

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
        var section = BuildMultiPage(fonts, out _, out _);

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.All(pages, p => Assert.All(p.Body.Lines,
            l => Assert.True(l.Y + l.Line.Height <= p.ContentBox.Height + Tol)));
    }

    [Fact]
    public void NoHeaderOrFooter_LeavesBandsEmpty()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(500, 400);
        section.Blocks.Add(PaginationSupport.Text("solo"));

        var page = IsolatedPaginator.PaginateIsolated(section, fonts)[0];

        Assert.Empty(page.HeaderLayer.Lines);
        Assert.Empty(page.FooterLayer.Lines);
    }
}
