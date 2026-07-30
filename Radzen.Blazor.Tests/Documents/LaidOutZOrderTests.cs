#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Geometry;
using Radzen.Documents.Layout;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class LaidOutZOrderTests
{
    private static Section Page(Document document, double width = 400, double height = 300)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(width), Unit.FromPoint(height));
        section.Margins.SetAll(Unit.FromPoint(20));
        return section;
    }

    private static IReadOnlyList<int> ZOrders(LaidOutLayer layer)
        =>
        [
            .. layer.Lines.Select(line => line.ZOrder),
            .. layer.Images.Select(image => image.ZOrder),
            .. layer.CodeSymbols.Select(code => code.ZOrder),
            .. layer.Tables.Select(table => table.ZOrder),
            .. layer.Boxes.Select(box => box.ZOrder),
        ];

    private static IReadOnlyList<int> ZOrders(LaidOutBoxContent content)
        =>
        [
            .. content.Lines.Select(line => line.ZOrder),
            .. content.Images.Select(image => image.ZOrder),
            .. content.CodeSymbols.Select(code => code.ZOrder),
            .. content.Tables.Select(table => table.ZOrder),
            .. content.Boxes.Select(box => box.ZOrder),
        ];

    [Fact]
    public void EveryBodyDraw_CarriesADistinctZOrder()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph().Inlines.Add("first");
        section.Blocks.AddParagraph().Inlines.Add("second");
        var container = section.Blocks.Add(new Container());
        container.Blocks.AddParagraph().Inlines.Add("inside");

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var orders = ZOrders(page.Body);

        Assert.Equal(orders.Count, orders.Distinct().Count());
    }

    [Fact]
    public void BodyLines_AreOrderedByDeclarationOrder()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph().Inlines.Add("first");
        section.Blocks.AddParagraph().Inlines.Add("second");
        section.Blocks.AddParagraph().Inlines.Add("third");

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var orders = page.Body.Lines.Select(line => line.ZOrder).ToArray();

        Assert.Equal(orders.OrderBy(order => order).ToArray(), orders);
    }

    [Fact]
    public void ADeclaredBoxOutranksThePrecedingParagraph()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph().Inlines.Add("before");
        var container = section.Blocks.Add(new Container());
        container.Blocks.AddParagraph().Inlines.Add("inside");

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);

        Assert.True(page.Body.Lines[^1].ZOrder < page.Body.Boxes[0].ZOrder);
    }

    [Fact]
    public void NestedContextsComposeIntoOneStackWithinTheirBox()
    {
        var document = new Document();
        var section = Page(document);
        var container = section.Blocks.Add(new Container());
        container.Layout = ContainerLayout.Overlay;
        container.Blocks.AddParagraph().Inlines.Add("under");
        var inner = container.Blocks.Add(new Container());
        inner.Blocks.AddParagraph().Inlines.Add("over");

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var content = page.Body.Boxes[0].Content;
        var orders = ZOrders(content);

        Assert.Equal(orders.Count, orders.Distinct().Count());
        Assert.True(content.Lines[0].ZOrder < content.Boxes[0].ZOrder);
    }

    [Fact]
    public void BandDraws_CarryTheirOwnStack()
    {
        var document = new Document();
        var section = Page(document);
        section.Header.Blocks.AddParagraph().Inlines.Add("header first");
        section.Header.Blocks.AddParagraph().Inlines.Add("header second");
        section.Blocks.AddParagraph().Inlines.Add("body");

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var orders = ZOrders(page.HeaderLayer);

        Assert.Equal(orders.Count, orders.Distinct().Count());
        Assert.Equal(orders.OrderBy(order => order).ToArray(), orders.ToArray());
    }
}
