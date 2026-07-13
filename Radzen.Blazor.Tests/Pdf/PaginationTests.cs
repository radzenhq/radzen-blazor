#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Core flow-pagination contract: exact page COUNT and per-line Y offsets for
// known-height paragraphs across a fixed page height, plus explicit PageBreak and
// SpacingBefore/After. See PaginationSupport for the pinned model + semantics.
public class PaginationTests
{
    private const double Tol = 1e-6;

    [Fact]
    public void TwelveSingleLineParagraphs_FivePerPage_ProduceThreePages()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 5));

        var paras = new Paragraph[12];
        for (var i = 0; i < paras.Length; i++)
        {
            paras[i] = section.Blocks.Add(PaginationSupport.Text($"P{i}"));
        }

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(3, pages.Count);
        Assert.Equal(5, pages[0].Lines.Count);
        Assert.Equal(5, pages[1].Lines.Count);
        Assert.Equal(2, pages[2].Lines.Count);
    }

    [Fact]
    public void PageNumbers_AreOneBasedAndSequential()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 5));
        for (var i = 0; i < 12; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"P{i}"));
        }

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal([1, 2, 3], pages.Select(p => p.Number).ToArray());
    }

    [Fact]
    public void EachBodyLine_AdvancesByLineHeightFromContentTop()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 5));
        for (var i = 0; i < 12; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"P{i}"));
        }

        var pages = Paginator.Paginate(section, fonts);

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(i * lineH, pages[0].Lines[i].Y, Tol);
            Assert.Equal(i * lineH, pages[1].Lines[i].Y, Tol);
        }

        Assert.Equal(0.0, pages[2].Lines[0].Y, Tol);
        Assert.Equal(lineH, pages[2].Lines[1].Y, Tol);
    }

    [Fact]
    public void FirstLineOfEveryPage_StartsAtContentTop()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 5));
        for (var i = 0; i < 12; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"P{i}"));
        }

        var pages = Paginator.Paginate(section, fonts);

        Assert.All(pages, p => Assert.Equal(0.0, p.Lines[0].Y, Tol));
    }

    [Fact]
    public void SourceReferences_MapLinesBackToTheirParagraphs()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineH, 5));
        var paras = new Paragraph[12];
        for (var i = 0; i < paras.Length; i++)
        {
            paras[i] = section.Blocks.Add(PaginationSupport.Text($"P{i}"));
        }

        var pages = Paginator.Paginate(section, fonts);

        Assert.Same(paras[0], pages[0].Lines[0].Source);
        Assert.Same(paras[4], pages[0].Lines[4].Source);
        Assert.Same(paras[5], pages[1].Lines[0].Source);
        Assert.Same(paras[10], pages[2].Lines[0].Source);
        Assert.Same(paras[11], pages[2].Lines[1].Source);
    }

    [Fact]
    public void ContentBox_EqualsPageMinusMargins()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var h = PaginationSupport.HeightForLines(lineH, 5);
        var section = PaginationSupport.Section(500, h, marginPt: 0);
        section.Margins.Left = Unit.FromPoint(20);
        section.Margins.Right = Unit.FromPoint(30);
        section.Margins.Top = Unit.FromPoint(15);
        section.Margins.Bottom = Unit.FromPoint(25);
        section.Blocks.Add(PaginationSupport.Text("only"));

        var page = Paginator.Paginate(section, fonts)[0];

        Assert.Equal(20.0, page.ContentBox.X, Tol);
        Assert.Equal(15.0, page.ContentBox.Y, Tol);
        Assert.Equal(500 - 20 - 30, page.ContentBox.Width, Tol);
        Assert.Equal(h - 15 - 25, page.ContentBox.Height, Tol);
    }

    [Fact]
    public void PageSize_IsReportedPerPage()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(360, 480);
        section.Blocks.Add(PaginationSupport.Text("x"));

        var page = Paginator.Paginate(section, fonts)[0];

        Assert.Equal(360.0, page.Size.Width.Point, Tol);
        Assert.Equal(480.0, page.Size.Height.Point, Tol);
    }

    [Fact]
    public void ExplicitPageBreak_StartsANewPage()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(500, 400);
        var a = section.Blocks.Add(PaginationSupport.Text("before"));
        section.Blocks.AddPageBreak();
        var b = section.Blocks.Add(PaginationSupport.Text("after"));

        var pages = Paginator.Paginate(section, fonts);

        Assert.Equal(2, pages.Count);
        Assert.Same(a, pages[0].Lines.Single().Source);
        Assert.Same(b, pages[1].Lines.Single().Source);
        Assert.Equal(0.0, pages[1].Lines[0].Y, Tol);
    }

    [Fact]
    public void SpacingBefore_OffsetsTheFirstLine()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(500, 400);
        var p = PaginationSupport.Text("spaced");
        p.SpacingBefore = Unit.FromPoint(7);
        section.Blocks.Add(p);

        var page = Paginator.Paginate(section, fonts)[0];

        Assert.Equal(7.0, page.Lines[0].Y, Tol);
    }

    [Fact]
    public void SpacingAfter_PushesTheFollowingParagraphDown()
    {
        var fonts = PaginationSupport.Fonts();
        var lineH = PaginationSupport.LineHeight(fonts);
        var section = PaginationSupport.Section(500, 400);
        var first = PaginationSupport.Text("first");
        first.SpacingAfter = Unit.FromPoint(9);
        section.Blocks.Add(first);
        var second = section.Blocks.Add(PaginationSupport.Text("second"));

        var page = Paginator.Paginate(section, fonts)[0];

        var secondLine = page.Lines.Single(l => ReferenceEquals(l.Source, second));
        Assert.Equal(lineH + 9, secondLine.Y, Tol);
    }

    [Fact]
    public void DocumentBuilderOverload_PaginatesItsSection()
    {
        var fonts = PaginationSupport.Fonts();
        var doc = new DocumentBuilder();
        var section = doc.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(500), Unit.FromPoint(400));
        section.Margin = Unit.FromPoint(0);
        section.Blocks.Add(PaginationSupport.Text("a"));
        section.Blocks.Add(PaginationSupport.Text("b"));
        section.Blocks.Add(PaginationSupport.Text("c"));

        var pages = Paginator.Paginate(doc, fonts);

        Assert.Single(pages);
        Assert.Equal(3, pages[0].Lines.Count);
    }

    [Fact]
    public void EachSection_StartsOnAFreshPage()
    {
        var fonts = PaginationSupport.Fonts();
        var doc = new DocumentBuilder();

        var first = doc.Sections.Add();
        first.PageSize = new PageSize(Unit.FromPoint(500), Unit.FromPoint(400));
        first.Margin = Unit.FromPoint(0);
        var pa = first.Blocks.Add(PaginationSupport.Text("alpha"));

        var second = doc.Sections.Add();
        second.PageSize = new PageSize(Unit.FromPoint(500), Unit.FromPoint(400));
        second.Margin = Unit.FromPoint(0);
        var pb = second.Blocks.Add(PaginationSupport.Text("beta"));

        var pages = Paginator.Paginate(doc, fonts);

        Assert.Equal(2, pages.Count);
        Assert.Same(pa, pages[0].Lines.Single().Source);
        Assert.Same(pb, pages[1].Lines.Single().Source);
    }
}
