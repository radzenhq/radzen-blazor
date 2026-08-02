#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

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

    private static IReadOnlyList<string> Merge(
        ImmutableArray<string> tables,
        Func<string, int> tableOrder,
        ImmutableArray<string> boxes,
        Func<string, int> boxOrder)
    {
        var visited = new List<string>();
        var cursor = OrderedMerge.ByOrder(tables, tableOrder, boxes, boxOrder);
        while (cursor.MoveNext())
        {
            visited.Add(cursor.IsTable ? tables[cursor.TableIndex] : boxes[cursor.BoxIndex]);
        }

        return visited;
    }

    [Fact]
    public void OrderedMerge_EmitsTheTableFirstWhenTheZOrdersTie()
    {
        var visited = Merge(
            ImmutableArray.Create("t0", "t5"),
            table => table == "t0" ? 0 : 5,
            ImmutableArray.Create("b0", "b5"),
            box => box == "b0" ? 0 : 5);

        Assert.Equal(new[] { "t0", "b0", "t5", "b5" }, visited);
    }

    [Fact]
    public void OrderedMerge_DrainsTheRemainingSideWhenOneRunsOut()
    {
        var visited = Merge(
            ImmutableArray.Create("t9"),
            _ => 9,
            ImmutableArray.Create("b1", "b2"),
            box => box == "b1" ? 1 : 2);

        Assert.Equal(new[] { "b1", "b2", "t9" }, visited);
    }

    [Fact]
    public void OrderedMerge_VisitsNothingWhenBothSidesAreEmpty()
    {
        Assert.Empty(Merge(
            ImmutableArray<string>.Empty,
            _ => 0,
            ImmutableArray<string>.Empty,
            _ => 0));
    }
}
