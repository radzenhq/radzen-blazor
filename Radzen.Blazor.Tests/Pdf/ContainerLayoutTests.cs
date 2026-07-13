#nullable enable
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Container is a decorated block: it wraps child blocks in a box with padding, background,
// borders, an optional fixed width and horizontal alignment. A Stack container is placed
// as a first-class box everywhere: section body (PaginatedPage.Boxes), header/footer bands
// (PaginatedPage.HeaderBoxes/FooterBoxes) and nested inside a cell or another box
// (LaidOutBoxContent.Boxes); only overlay containers still lower onto the table engine.
// Child content is inset by the padding and the box decoration is drawn exactly like a cell's.
public class ContainerLayoutTests
{
    private static Paragraph Text(string text, double size = 12)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = PaginationSupport.Family;
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

        var pages = Paginator.Paginate(section, fonts);

        var page = Assert.Single(pages);
        Assert.Empty(page.Tables);
        var box = Assert.Single(page.Boxes);
        Assert.Equal(400, box.Bounds.Width, 6);
        Assert.Equal(0, box.Bounds.X, 6);
        Assert.Equal(box.Content.Height + 20, box.Bounds.Height, 6);

        var line = Assert.Single(box.Content.Lines);
        Assert.Equal(10, line.X, 6);
        Assert.Equal(10, line.Y, 6);

        Assert.Equal(container.Background, box.Style.Background);
        Assert.Equal(2, box.Style.Top.Width, 6);
        Assert.Equal(2, box.Style.Left.Width, 6);
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

        var pages = Paginator.Paginate(section, fonts);
        var page = Assert.Single(pages);

        var box = Assert.Single(page.HeaderBoxes);
        Assert.Equal(0, box.Y, 6);
        Assert.Equal(box.Content.Height + 12, box.Bounds.Height, 6);
        Assert.Null(box.Transform);

        // The band table follows the box: placed below it and ordered after it.
        var fragment = Assert.Single(page.HeaderTables);
        Assert.Equal(box.Bounds.Height, fragment.Y, 6);
        Assert.True(box.Order < fragment.Order, "band box precedes the band table");
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

        var pages = Paginator.Paginate(section, fonts);

        var box = Assert.Single(Assert.Single(pages).Boxes);
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

        var pages = Paginator.Paginate(section, fonts);

        var box = Assert.Single(Assert.Single(pages).Boxes);
        Assert.Empty(box.Content.Tables);
        var nested = Assert.Single(box.Content.Boxes);

        Assert.Same(inner, nested.Source);
        Assert.Equal(12, nested.Bounds.X, 6);
        Assert.Equal(400 - 24, nested.Bounds.Width, 6);
        var line = Assert.Single(nested.Content.Lines);
        Assert.Equal(5, line.X, 6);
        Assert.Equal(5, line.Y, 6);
    }

    [Fact]
    public void Container_BuildsToPdf_WithBoxAndText()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(255, 255, 0),
        });
        container.Borders.Width = 1;
        var paragraph = container.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("Inside the box");
        run.Font.Name = BuildTestSupport.Latin;

        var document = builder.Build();

        var page = Assert.Single(document.Pages);
        Assert.Contains("Inside the box", page.ExtractText());
        var content = Encoding.ASCII.GetString(page.GetContent()!);
        Assert.Contains("re f", content);
        Assert.Contains("RG", content);
    }

    [Fact]
    public void Container_ChildRuns_GetStyleResolvedFonts()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container());
        var paragraph = container.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("styled");

        StyleResolver.Resolve(builder);

        Assert.NotNull(run.EffectiveFont);
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
        cellParagraph.Inlines.Add("nested cell").Font.Name = PaginationSupport.Family;

        var pages = Paginator.Paginate(section, fonts);

        var page = Assert.Single(pages);
        Assert.Empty(page.Tables);
        var box = Assert.Single(page.Boxes);
        Assert.Equal(2, box.Content.Lines.Count);
        Assert.Single(box.Content.Tables);
        Assert.Equal(box.Content.Height + 12, box.Bounds.Height, 6);
        Assert.Equal(container.Background, box.Style.Background);
        Assert.Equal(4, box.Style.CornerRadius.Point, 6);

        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var buildSection = builder.Sections.Add();
        var buildContainer = buildSection.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(6),
            Background = Color.FromRgb(230, 240, 250),
            CornerRadius = Unit.FromPoint(4),
        });
        buildContainer.Borders.Width = 1;
        var first = buildContainer.Blocks.AddParagraph();
        first.Inlines.Add("First paragraph").Font.Name = BuildTestSupport.Latin;
        var buildNested = buildContainer.Blocks.AddTable();
        buildNested.Columns.Add(Unit.FromPoint(120));
        buildNested.Rows.Add().Cells[0].Blocks.AddParagraph().Inlines.Add("nested cell").Font.Name = BuildTestSupport.Latin;

        var document = builder.Build();

        var pdfPage = Assert.Single(document.Pages);
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
        deep.Inlines.Add("deep cell").Font.Name = PaginationSupport.Family;
        cell.Blocks.Add(container);

        var layout = TableLayout.Layout(table, 300, fonts);

        var laidCell = Assert.Single(layout.Cells);
        var box = Assert.Single(laidCell.Boxes);
        Assert.Same(container, box.Source);
        Assert.Equal(300, box.Bounds.Width, 6);
        Assert.Equal(4, box.Radius, 6);
        Assert.Equal(container.Background, box.Style.Background);
        var line = Assert.Single(box.Content.Lines);
        Assert.Equal(8, line.X, 6);
        Assert.Equal(8, line.Y, 6);
        var nestedTable = Assert.Single(box.Content.Tables);
        Assert.Same(innerTable, nestedTable.Layout.Source);
        Assert.Equal(box.Content.Height + 16, box.Bounds.Height, 6);

        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
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
        boxed.Inlines.Add("inside box").Font.Name = BuildTestSupport.Latin;
        var buildInner = buildContainer.Blocks.AddTable();
        buildInner.Columns.Add(Unit.FromPoint(100));
        buildInner.Rows.Add().Cells[0].Blocks.AddParagraph().Inlines.Add("deep cell").Font.Name = BuildTestSupport.Latin;
        buildCell.Blocks.Add(buildContainer);

        var document = builder.Build();

        var page = Assert.Single(document.Pages);
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

        var layout = TableLayout.Layout(table, 300, fonts);

        var outerBox = Assert.Single(Assert.Single(layout.Cells).Boxes);
        Assert.Same(outer, outerBox.Source);
        var innerBox = Assert.Single(outerBox.Content.Boxes);
        Assert.Same(inner, innerBox.Source);
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
            var builder = new DocumentBuilder();
            BuildTestSupport.RegisterLatin(builder);
            var section = builder.Sections.Add();
            var paragraph = section.Blocks.AddParagraph();
            paragraph.Inlines.Add("Plain body text").Font.Name = BuildTestSupport.Latin;
            var table = section.Blocks.AddTable();
            table.Borders.Width = 0.5;
            table.Columns.Add(Unit.FromPoint(120));
            table.Rows.Add().Cells[0].Blocks.AddParagraph().Inlines.Add("cell").Font.Name = BuildTestSupport.Latin;
            return builder.ToArray();
        }

        Assert.Equal(Build(), Build());
    }

    [Fact]
    public void DocumentWithoutContainers_ExpandsBlocksUnchanged()
    {
        var section = PaginationSupport.Section(400, 600);
        var paragraph = section.Blocks.Add(Text("plain"));

        var expanded = Paginator.ExpandBlocks(section.Blocks, 400);

        Assert.Same(paragraph, Assert.Single(expanded));
    }
}
