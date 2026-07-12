#nullable enable
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Container is a decorated block: it wraps child blocks in a box with padding, background,
// borders, an optional fixed width and horizontal alignment. A section-level Stack container
// is placed as a first-class box (PaginatedPage.Boxes); nested, band, overlay and rotated
// containers still lower onto the table engine. Child content is inset by the padding and
// the box decoration is drawn exactly like a cell's.
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
        var nested = Assert.Single(box.Content.Tables);
        var innerCell = Assert.Single(nested.Layout.Cells);

        Assert.Equal(12, nested.X, 6);
        Assert.Equal(400 - 24, nested.Layout.Width, 6);
        Assert.Equal(5, innerCell.ContentBox.Left - innerCell.Bounds.Left, 6);
        var line = Assert.Single(innerCell.Lines);
        Assert.Equal(innerCell.ContentBox.Left, line.X, 6);
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
