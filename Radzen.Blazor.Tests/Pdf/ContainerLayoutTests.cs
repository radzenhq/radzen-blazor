#nullable enable
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Blazor.Tests.Isolated;
namespace Radzen.Blazor.Pdf.Tests;

public class ContainerLayoutTests
{
    private static Paragraph Text(string text, double size = 12)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Family = PaginationSupport.Family;
        run.Font.Size = size;
        return paragraph;
    }

    [Fact]
    public void Container_InsetsChildByPadding_AndCarriesDecoration()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(230, 230, 230),
        });
        container.Borders.Width = 2;
        container.Blocks.Add(Text("Boxed"));

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        var page = Assert.Single(pages);
        Assert.Empty(page.Body.Tables);
        var box = Assert.Single(page.Body.Boxes);
        Assert.Equal(400, box.Bounds.Width, 6);
        Assert.Equal(0, box.Bounds.X, 6);
        Assert.Equal(box.Content.Height + 20, box.Bounds.Height, 6);

        var line = Assert.Single(box.Content.Lines);
        Assert.Equal(10, line.X, 6);
        Assert.Equal(10, line.Y, 6);

        Assert.Equal(container.Background, box.Style.Background);
        Assert.Equal(2, box.Style.Top!.Value.Width, 6);
        Assert.Equal(2, box.Style.Left!.Value.Width, 6);
    }

    [Fact]
    public void Container_PerSidePaddingOverridesTheScalarEdgeByEdge()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            PaddingLeft = Unit.FromPoint(4),
            PaddingTop = Unit.FromPoint(6),
            PaddingBottom = Unit.FromPoint(20),
        });
        container.Blocks.Add(Text("Boxed"));

        var box = Assert.Single(Assert.Single(IsolatedPaginator.PaginateIsolated(section, fonts)).Body.Boxes);
        var line = Assert.Single(box.Content.Lines);

        Assert.Equal(4, line.X, 6);
        Assert.Equal(6, line.Y, 6);
        Assert.Equal(box.Content.Height + 26, box.Bounds.Height, 6);
    }

    [Fact]
    public void Container_UnsetPerSidePaddingFallsBackToTheScalar()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(10) });
        container.Blocks.Add(Text("Boxed"));

        var box = Assert.Single(Assert.Single(IsolatedPaginator.PaginateIsolated(section, fonts)).Body.Boxes);
        var line = Assert.Single(box.Content.Lines);

        Assert.Null(container.PaddingLeft);
        Assert.Null(container.PaddingRight);
        Assert.Null(container.PaddingTop);
        Assert.Null(container.PaddingBottom);
        Assert.Equal(10, line.X, 6);
        Assert.Equal(10, line.Y, 6);
        Assert.Equal(box.Content.Height + 20, box.Bounds.Height, 6);
    }

    [Fact]
    public void ContainerInHeaderBand_PlacesAsBandBox_InterleavedWithBandTables()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Header.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(6),
            Background = Color.FromRgb(240, 240, 240),
        });
        container.Blocks.Add(Text("Band box"));
        var table = section.Header.Blocks.AddTable();
        table.Columns.Add();
        table.Rows.Add().Cells[0].Blocks.Add(Text("Band table"));
        section.Blocks.Add(Text("Body"));

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);
        var page = Assert.Single(pages);

        var box = Assert.Single(page.HeaderLayer.Boxes);
        Assert.Equal(0, box.Bounds.Y, 6);
        Assert.Equal(box.Content.Height + 12, box.Bounds.Height, 6);
        Assert.Null(box.Transform);

        var fragment = Assert.Single(page.HeaderLayer.Tables);
        Assert.Equal(box.Bounds.Height, fragment.Bounds.Y, 6);
        Assert.True(box.ZOrder < fragment.ZOrder, "band box precedes the band table");
    }

    [Fact]
    public void Container_WithWidthAndCenterAlignment_OffsetsTheBox()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(200),
            Alignment = HorizontalAlignment.Center,
        });
        container.Blocks.Add(Text("Centered box"));

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        var box = Assert.Single(Assert.Single(pages).Body.Boxes);
        Assert.Equal(200, box.Bounds.Width, 6);
        Assert.Equal(100, box.Bounds.X, 6);
    }

    [Fact]
    public void NestedContainers_InsetByBothPaddings()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var outer = section.Blocks.Add(new Container { Padding = Unit.FromPoint(12) });
        var inner = outer.Blocks.Add(new Container { Padding = Unit.FromPoint(5) });
        inner.Blocks.Add(Text("Deep"));

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);

        var box = Assert.Single(Assert.Single(pages).Body.Boxes);
        Assert.Empty(box.Content.Tables);
        var nested = Assert.Single(box.Content.Boxes);

        Assert.Equal(capture.Source(inner), nested.Source);
        Assert.Equal(12, nested.Bounds.X, 6);
        Assert.Equal(400 - 24, nested.Bounds.Width, 6);
        var line = Assert.Single(nested.Content.Lines);
        Assert.Equal(5, line.X, 6);
        Assert.Equal(5, line.Y, 6);
    }

    [Fact]
    public void Container_BuildsToPdf_WithBoxAndText()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(255, 255, 0),
        });
        container.Borders.Width = 1;
        var paragraph = container.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("Inside the box");
        run.Font.Family = BuildTestSupport.Latin;

        var pdf = new DocumentRenderer().Render(document);

        var page = Assert.Single(pdf.Pages);
        Assert.Contains("Inside the box", page.ExtractText());
        var content = Encoding.ASCII.GetString(page.GetContent()!);
        Assert.Contains("re f", content);
        Assert.Contains("RG", content);
    }

    [Fact]
    public void Container_ChildRuns_GetStyleResolvedFonts()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container());
        var paragraph = container.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("styled");

        var resolution = StyleResolver.Resolve(document);

        Assert.NotNull(resolution.RunFont(run));
    }

    [Fact]
    public void StackContainer_PaginatesAsBox_AndEmitsDecorationAndContent()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(6),
            Background = Color.FromRgb(230, 240, 250),
            CornerRadius = Unit.FromPoint(4),
        });
        container.Borders.Width = 1;
        container.Blocks.Add(Text("First paragraph"));
        container.Blocks.Add(Text("Second paragraph"));
        var nested = container.Blocks.AddTable();
        nested.Columns.Add(Unit.FromPoint(120));
        var cellParagraph = nested.Rows.Add().Cells[0].Blocks.AddParagraph();
        cellParagraph.Inlines.Add("nested cell").Font.Family = PaginationSupport.Family;

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        var page = Assert.Single(pages);
        Assert.Empty(page.Body.Tables);
        var box = Assert.Single(page.Body.Boxes);
        Assert.Equal(2, box.Content.Lines.Length);
        Assert.Single(box.Content.Tables);
        Assert.Equal(box.Content.Height + 12, box.Bounds.Height, 6);
        Assert.Equal(container.Background, box.Style.Background);
        Assert.Equal(4, box.Style.CornerRadius, 6);

        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var buildSection = document.Sections.Add();
        var buildContainer = buildSection.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(6),
            Background = Color.FromRgb(230, 240, 250),
            CornerRadius = Unit.FromPoint(4),
        });
        buildContainer.Borders.Width = 1;
        var first = buildContainer.Blocks.AddParagraph();
        first.Inlines.Add("First paragraph").Font.Family = BuildTestSupport.Latin;
        var buildNested = buildContainer.Blocks.AddTable();
        buildNested.Columns.Add(Unit.FromPoint(120));
        buildNested.Rows.Add().Cells[0].Blocks.AddParagraph().Inlines.Add("nested cell").Font.Family = BuildTestSupport.Latin;

        var pdf = new DocumentRenderer().Render(document);

        var pdfPage = Assert.Single(pdf.Pages);
        var extracted = pdfPage.ExtractText();
        Assert.Contains("First paragraph", extracted);
        Assert.Contains("nested cell", extracted);
        var content = Encoding.ASCII.GetString(pdfPage.GetContent()!);
        Assert.Contains(" rg", content);
        Assert.Contains(" RG", content);
    }

    [Fact]
    public void ContainerInTableCell_LaysOutAsNestedBox_AndBuildsDecorationAndContent()
    {
        var fonts = PaginationSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(300));
        var cell = table.Rows.Add().Cells[0];
        cell.Blocks.Add(Text("before"));
        var container = new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 240, 250),
            CornerRadius = Unit.FromPoint(4),
        };
        container.Borders.Width = 1;
        container.Blocks.Add(Text("inside box"));
        var innerTable = container.Blocks.AddTable();
        innerTable.Columns.Add(Unit.FromPoint(100));
        var deep = innerTable.Rows.Add().Cells[0].Blocks.AddParagraph();
        deep.Inlines.Add("deep cell").Font.Family = PaginationSupport.Family;
        cell.Blocks.Add(container);

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts, capture: capture);

        var laidCell = Assert.Single(layout.Cells);
        var box = Assert.Single(laidCell.Boxes);
        Assert.Equal(capture.Source(container), box.Source);
        Assert.Equal(300, box.Bounds.Width, 6);
        Assert.Equal(4, box.Style.CornerRadius, 6);
        Assert.Equal(container.Background, box.Style.Background);
        var line = Assert.Single(box.Content.Lines);
        Assert.Equal(8, line.X, 6);
        Assert.Equal(8, line.Y, 6);
        var nestedTable = Assert.Single(box.Content.Tables);
        Assert.Equal(capture.Source(innerTable), nestedTable.Layout.Source);
        Assert.Equal(box.Content.Height + 16, box.Bounds.Height, 6);

        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var buildTable = section.Blocks.AddTable();
        buildTable.Borders.Width = 0.5;
        buildTable.Columns.Add(Unit.FromPoint(300));
        var buildCell = buildTable.Rows.Add().Cells[0];
        var buildContainer = new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 240, 250),
            CornerRadius = Unit.FromPoint(4),
        };
        buildContainer.Borders.Width = 1;
        var boxed = buildContainer.Blocks.AddParagraph();
        boxed.Inlines.Add("inside box").Font.Family = BuildTestSupport.Latin;
        var buildInner = buildContainer.Blocks.AddTable();
        buildInner.Columns.Add(Unit.FromPoint(100));
        buildInner.Rows.Add().Cells[0].Blocks.AddParagraph().Inlines.Add("deep cell").Font.Family = BuildTestSupport.Latin;
        buildCell.Blocks.Add(buildContainer);

        var pdf = new DocumentRenderer().Render(document);

        var page = Assert.Single(pdf.Pages);
        var extracted = page.ExtractText();
        Assert.Contains("inside box", extracted);
        Assert.Contains("deep cell", extracted);
        var content = Encoding.ASCII.GetString(page.GetContent()!);
        Assert.Contains(" rg", content);
        Assert.Contains(" RG", content);
    }

    [Fact]
    public void ContainerInContainer_InsideCell_NestsTwoLevelsOfBoxes()
    {
        var fonts = PaginationSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(300));
        var cell = table.Rows.Add().Cells[0];
        var outer = new Container { Padding = Unit.FromPoint(10) };
        var inner = new Container { Padding = Unit.FromPoint(5), Background = Color.FromRgb(255, 255, 200) };
        inner.Blocks.Add(Text("two deep"));
        outer.Blocks.Add(inner);
        cell.Blocks.Add(outer);

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts, capture: capture);

        var outerBox = Assert.Single(Assert.Single(layout.Cells).Boxes);
        Assert.Equal(capture.Source(outer), outerBox.Source);
        var innerBox = Assert.Single(outerBox.Content.Boxes);
        Assert.Equal(capture.Source(inner), innerBox.Source);
        Assert.Equal(300 - 20, innerBox.Bounds.Width, 6);
        var line = Assert.Single(innerBox.Content.Lines);
        Assert.Equal(5, line.X, 6);
        Assert.Equal(5, line.Y, 6);
    }

    [Fact]
    public void NonContainerDocument_BuildsByteIdenticalTwice()
    {
        static byte[] Build()
        {
            var document = new Document();
            BuildTestSupport.RegisterLatin(document);
            var section = document.Sections.Add();
            var paragraph = section.Blocks.AddParagraph();
            paragraph.Inlines.Add("Plain body text").Font.Family = BuildTestSupport.Latin;
            var table = section.Blocks.AddTable();
            table.Borders.Width = 0.5;
            table.Columns.Add(Unit.FromPoint(120));
            table.Rows.Add().Cells[0].Blocks.AddParagraph().Inlines.Add("cell").Font.Family = BuildTestSupport.Latin;
            return new DocumentRenderer().ToArray(document);
        }

        Assert.Equal(Build(), Build());
    }

    [Fact]
    public void DocumentWithoutContainers_ExpandsBlocksUnchanged()
    {
        var section = PaginationSupport.Section(400, 600);
        var paragraph = section.Blocks.Add(Text("plain"));

        var expanded = IsolatedBlockExpander.ExpandBlocksIsolated(section.Blocks, 400);

        Assert.Same(paragraph, Assert.Single(expanded));
    }
}
