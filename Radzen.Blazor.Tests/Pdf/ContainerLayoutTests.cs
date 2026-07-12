#nullable enable
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Container is a decorated block: it wraps child blocks in a box with padding, background,
// borders, an optional fixed width and horizontal alignment. It lowers onto the table engine
// (a single-cell table), so its child content is inset by the padding and the box decoration
// is drawn exactly like a cell's.
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

        var fragment = Assert.Single(Assert.Single(pages).Tables);
        var cell = Assert.Single(fragment.Layout.Cells);
        Assert.Equal(400, fragment.Layout.Width, 6);
        Assert.Equal(10, cell.ContentBox.Left - cell.Bounds.Left, 6);
        Assert.Equal(10, cell.ContentBox.Top - cell.Bounds.Top, 6);
        Assert.Equal(cell.Bounds.Width - 20, cell.ContentBox.Width, 6);
        Assert.Equal(cell.Bounds.Height - 20, cell.ContentBox.Height, 6);

        var line = Assert.Single(cell.Lines);
        Assert.Equal(cell.ContentBox.Left, line.X, 6);
        Assert.Equal(cell.ContentBox.Top, line.Y, 6);

        Assert.Equal(container.Background, cell.Cell.Background);
        Assert.Equal(2, cell.Cell.Borders.Top.Width, 6);
        Assert.Equal(2, cell.Cell.Borders.Left.Width, 6);
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

        var fragment = Assert.Single(Assert.Single(pages).Tables);
        Assert.Equal(200, fragment.Layout.Width, 6);
        Assert.Equal(100, fragment.Layout.Source!.LeftIndent.Point, 6);
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

        var fragment = Assert.Single(Assert.Single(pages).Tables);
        var outerCell = Assert.Single(fragment.Layout.Cells);
        var nested = Assert.Single(outerCell.Tables);
        var innerCell = Assert.Single(nested.Layout.Cells);

        Assert.Equal(12, outerCell.ContentBox.Left, 6);
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
    public void DocumentWithoutContainers_ExpandsBlocksUnchanged()
    {
        var section = PaginationSupport.Section(400, 600);
        var paragraph = section.Blocks.Add(Text("plain"));

        var expanded = Paginator.ExpandBlocks(section.Blocks, 400);

        Assert.Same(paragraph, Assert.Single(expanded));
    }
}
