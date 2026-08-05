#nullable enable
using System.Linq;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class ListItemBlockContentTests
{
    private const double Tol = 1e-6;

    [Fact]
    public void Blocks_UseTheSharedEditableBlockCollectionRules()
    {
        var item = new ListItem();
        var first = item.Blocks.Add(new Paragraph("first"));
        var table = item.Blocks.Add(new Table());
        item.Blocks.Insert(1, new Paragraph { Text = "second" });

        item.Blocks.Move(2, 0);
        Assert.Same(table, item.Blocks[0]);
        Assert.True(item.Blocks.Remove(first));

        var other = new ListItem();
        Assert.Throws<InvalidOperationException>(() => other.Blocks.Add(table));

        item.Blocks.Remove(table);
        other.Blocks.Add(table);
        Assert.Same(table, other.Blocks[0]);

        other.Blocks.Clear();
        Assert.Empty(other.Blocks);
    }

    [Fact]
    public void Blocks_RejectAListAttachedToItsOwnItem()
    {
        var list = new ListBlock();
        var item = list.AddItem("item");

        Assert.Throws<InvalidOperationException>(() => item.Blocks.Add(list));
    }

    [Fact]
    public void TextInlinesAndNestedList_RemainTerseCompatibilityPaths()
    {
        var list = new ListBlock();
        var item = list.AddItem("one");
        item.Inlines.Add(" two");
        var nested = item.NestedList = new ListBlock { Style = ListStyle.Number };

        Assert.Equal("one two", item.Text);
        Assert.IsType<Paragraph>(item.Blocks[0]);
        Assert.Same(nested, item.Blocks[1]);
        Assert.Same(nested, item.NestedList);
    }

    [Fact]
    public void MultiBlockItem_IndentsEveryBlockAndNestedListFromTheContentEdge()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));

        var list = section.Blocks.Add(new ListBlock());
        list.HangingIndent = Unit.FromPoint(20);
        var item = list.AddItem();
        item.Blocks.Add(PaginationSupport.Text("first"));
        item.Blocks.Add(PaginationSupport.Text("second"));
        var nested = item.Blocks.Add(new ListBlock());
        nested.HangingIndent = Unit.FromPoint(15);
        nested.AddItem("inner").Font.Family = PaginationSupport.Family;

        document.Fonts.Register(
            PaginationSupport.Family,
            new System.IO.MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        var lines = Assert.Single(DocumentLayouter.Layout(document).Pages).Body.Lines;

        var first = lines.Single(line => Text(line) == "first");
        var second = lines.Single(line => Text(line) == "second");
        var inner = lines.Single(line => Text(line) == "inner");
        Assert.Equal(20, ContentX(first), Tol);
        Assert.Equal(20, ContentX(second), Tol);
        Assert.Equal(35, ContentX(inner), Tol);
    }

    [Fact]
    public void RepeatedMeasurement_UsesTheMarkerIndentFromTheFinalPlacement()
    {
        var fonts = PaginationSupport.Fonts();
        var resolution = LoweringResult.CreateForDocument(StyleResolution.Empty);
        var capture = new LayoutCaptureContext(ImageProbes.None);

        var narrow = new Container { Width = Unit.FromPoint(120) };
        var narrowOuter = narrow.Blocks.Add(new ListBlock());
        narrowOuter.LeftIndent = Unit.FromPoint(5);
        narrowOuter.HangingIndent = Unit.FromPoint(10);
        var narrowItem = narrowOuter.AddItem("narrow");
        var measuredList = narrowItem.Blocks.Add(new ListBlock());
        measuredList.LeftIndent = Unit.FromPoint(3);
        measuredList.HangingIndent = Unit.FromPoint(7);
        measuredList.AddItem("target").Font.Family = PaginationSupport.Family;

        _ = BoxContentLayout.Measure(
            BlockCollection.Borrowing(narrow),
            120,
            null,
            fonts,
            resolution,
            capture);

        Assert.True(narrowItem.Blocks.Remove(measuredList));
        var wide = new Container { Width = Unit.FromPoint(240) };
        var wideOuter = wide.Blocks.Add(new ListBlock());
        wideOuter.LeftIndent = Unit.FromPoint(40);
        wideOuter.HangingIndent = Unit.FromPoint(20);
        var wideItem = wideOuter.AddItem("wide");
        wideItem.Blocks.Add(measuredList);

        var laidOut = BoxContentLayout.Layout(
            BlockCollection.Borrowing(wide),
            new Rect(0, 0, 240, 200),
            HorizontalAlignment.Left,
            VerticalAlignment.Top,
            fonts,
            resolution,
            capture);
        var target = Assert.Single(Assert.Single(laidOut.Boxes).Content.Lines.Where(line => Text(line) == "target"));
        var marker = target.Line.Fragments.Single(fragment => fragment.IsMarker);

        Assert.Equal(63, marker.XOffset, Tol);
        Assert.Equal(marker.XOffset + measuredList.HangingIndent.Point, ContentX(target), Tol);
    }

    [Fact]
    public void TableBlock_UsesTheItemContentEdge()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));

        var item = section.Blocks.Add(new ListBlock()).AddItem("intro");
        var table = item.Blocks.Add(new Table());
        table.Columns.Add();
        table.Rows.Add().Cells[0].Text = "cell";

        var fragment = Assert.Single(Assert.Single(DocumentLayouter.Layout(document).Pages).Body.Tables);

        Assert.Equal(18, fragment.Layout.Decoration.LeftIndent, Tol);
    }

    [Fact]
    public void ImageFirstItem_PlacesTheLabelAtTheImageTop()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));

        var item = section.Blocks.Add(new ListBlock()).AddItem();
        var image = item.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.Width = Unit.FromPoint(20);
        image.Height = Unit.FromPoint(20);

        var body = Assert.Single(DocumentLayouter.Layout(document).Pages).Body;
        var positionedImage = Assert.Single(body.Images);
        var marker = Assert.Single(body.Lines);

        Assert.Equal(18, positionedImage.X, Tol);
        Assert.Equal(positionedImage.Y, marker.Y, Tol);
        Assert.Contains(marker.Line.Fragments, fragment => fragment.IsMarker);
    }

    [Fact]
    public void ItemBlocks_SplitAcrossPagesThroughTheNormalFlow()
    {
        var fonts = PaginationSupport.Fonts();
        var lineHeight = PaginationSupport.LineHeight();
        var section = PaginationSupport.Section(500, PaginationSupport.HeightForLines(lineHeight, 3));
        section.Blocks.Add(PaginationSupport.Text("f0"));
        section.Blocks.Add(PaginationSupport.Text("f1"));
        var item = section.Blocks.Add(new ListBlock()).AddItem();
        var blocks = Enumerable.Range(0, 4)
            .Select(i => item.Blocks.Add(PaginationSupport.Text($"i{i}")))
            .ToArray();

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(2, pages.Length);
        Assert.Equal(blocks[0].Text, Text(pages[0].Body.Lines[^1]));
        Assert.Equal(
            3,
            pages[1].Body.Lines.Count(line => blocks.Any(block => block.Text == Text(line))));
        Assert.Single(pages.SelectMany(page => page.Body.Lines)
            .SelectMany(line => line.Line.Fragments)
            .Where(fragment => fragment.IsMarker));
    }

    [Fact]
    public void Label_MovesWithTheFirstContentLine()
    {
        var fonts = PaginationSupport.Fonts();
        var lineHeight = PaginationSupport.LineHeight();
        var width = PaginationSupport.WidthForWordsPerLine(fonts, "Ha", 2, 12);
        var section = PaginationSupport.Section(width + 18, PaginationSupport.HeightForLines(lineHeight, 2));
        section.Blocks.Add(PaginationSupport.Text("filler"));
        var item = section.Blocks.Add(new ListBlock()).AddItem();
        var paragraph = item.Blocks.Add(PaginationSupport.Repeated("Ha", 4));

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(2, pages.Length);
        Assert.DoesNotContain(pages[0].Body.Lines.SelectMany(line => line.Line.Fragments), fragment => fragment.IsMarker);
        var firstLine = pages[1].Body.Lines.First(line => line.Line.Fragments.Any(
            fragment => !fragment.IsMarker));
        Assert.Contains(firstLine.Line.Fragments, fragment => fragment.IsMarker);
        Assert.Equal(0, firstLine.Y, Tol);
    }

    [Fact]
    public void NamedItemStyleKeepTogether_MovesAllItemBlocks()
    {
        var document = new Document();
        var fonts = document.Fonts;
        fonts.Register(
            PaginationSupport.Family,
            new System.IO.MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        var lineHeight = PaginationSupport.LineHeight();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(
            Unit.FromPoint(500),
            Unit.FromPoint(PaginationSupport.HeightForLines(lineHeight, 3)));
        section.Margins.SetAll(Unit.FromPoint(0));
        section.Blocks.Add(PaginationSupport.Text("f0"));
        section.Blocks.Add(PaginationSupport.Text("f1"));

        document.Styles.Add("Keep list item").KeepTogether = true;
        var item = section.Blocks.Add(new ListBlock()).AddItem();
        item.StyleName = "Keep list item";
        var first = item.Blocks.Add(PaginationSupport.Text("i0"));
        var second = item.Blocks.Add(PaginationSupport.Text("i1"));

        var pages = DocumentLayouter.Layout(document).Pages;

        Assert.Equal(2, pages.Length);
        Assert.DoesNotContain(
            pages[0].Body.Lines,
            line => Text(line) == first.Text);
        Assert.Equal(
            new[] { first.Text, second.Text },
            pages[1].Body.Lines
                .Select(Text)
                .ToArray());
    }

    [Fact]
    public void Semantics_KeepOneListBodyWithTheBlockStructure()
    {
        var document = new Document();
        var item = document.Sections.Add().Blocks.Add(new ListBlock()).AddItem();
        item.Blocks.Add(new Paragraph("first"));
        item.Blocks.Add(new Paragraph("second"));
        item.Blocks.Add(new ListBlock { Style = ListStyle.Number }).AddItem("nested");

        var structure = DocumentLayouter.Layout(document).Semantics.Structure;
        var body = Assert.Single(structure.Nodes.Where(node => node.Intent == SemanticIntent.ListBody)
            .Where(node => node.Children.Length == 3));
        var children = body.Children.Select(index => structure.Nodes[index].Intent).ToArray();

        Assert.Equal(
            [SemanticIntent.Paragraph, SemanticIntent.Paragraph, SemanticIntent.List],
            children);
    }

    private static string Text(LaidOutLine line)
        => string.Concat(line.Line.Fragments.Where(fragment => !fragment.IsMarker).Select(fragment => fragment.Text));

    private static double ContentX(LaidOutLine line)
        => line.Line.Fragments.First(fragment => !fragment.IsMarker).XOffset;
}
