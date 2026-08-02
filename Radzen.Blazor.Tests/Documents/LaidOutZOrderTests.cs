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

    private static List<string> PaintOrder(Document document)
    {
        var reader = Radzen.Blazor.Pdf.Tests.BuildTestSupport.Read(document);
        var content = Radzen.Blazor.Pdf.Tests.ContentTestHelpers.PageContent(reader, 0);
        var painted = new List<string>();
        foreach (var operation in Radzen.Blazor.Pdf.Tests.ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator == "Tj")
            {
                painted.Add(System.Text.Encoding.ASCII.GetString(operation.Operands[0].Bytes!));
            }
            else if (operation.Operator == "f")
            {
                painted.Add("fill");
            }
        }

        return painted;
    }

    private static Container Panel(BlockCollection blocks, string text)
    {
        var container = blocks.Add(new Container { Background = Color.FromRgb(200, 200, 200) });
        container.Blocks.AddParagraph().Inlines.Add(text);
        return container;
    }

    [Fact]
    public void BodyDraws_PaintInDeclarationOrderAcrossLinesAndBoxes()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph().Inlines.Add("first");
        section.Blocks.AddParagraph().Inlines.Add("second");
        Panel(section.Blocks, "inside");

        Assert.Equal(["first", "second", "fill", "inside"], PaintOrder(document));
    }

    [Fact]
    public void BodyLines_PaintInDeclarationOrder()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph().Inlines.Add("first");
        section.Blocks.AddParagraph().Inlines.Add("second");
        section.Blocks.AddParagraph().Inlines.Add("third");

        Assert.Equal(["first", "second", "third"], PaintOrder(document));
    }

    [Fact]
    public void ADeclaredBox_PaintsOverThePrecedingParagraph()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph().Inlines.Add("before");
        Panel(section.Blocks, "inside");

        var painted = PaintOrder(document);

        Assert.True(painted.IndexOf("fill") > painted.IndexOf("before"));
    }

    [Fact]
    public void NestedOverlayContexts_PaintOuterContentBeforeTheInnerBox()
    {
        var document = new Document();
        var section = Page(document);
        var container = section.Blocks.Add(new Container { Layout = ContainerLayout.Overlay });
        container.Blocks.AddParagraph().Inlines.Add("under");
        Panel(container.Blocks, "over");

        var painted = PaintOrder(document);

        Assert.True(painted.IndexOf("under") < painted.IndexOf("fill"));
        Assert.True(painted.IndexOf("fill") < painted.IndexOf("over"));
    }

    [Fact]
    public void BandDraws_PaintInTheirOwnDeclarationOrder()
    {
        var document = new Document();
        var section = Page(document);
        section.Header.Blocks.AddParagraph().Inlines.Add("header first");
        section.Header.Blocks.AddParagraph().Inlines.Add("header second");
        section.Blocks.AddParagraph().Inlines.Add("body");

        var painted = PaintOrder(document);

        Assert.Contains("body", painted);
        Assert.True(painted.IndexOf("header first") < painted.IndexOf("header second"));
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

    [Theory]
    [InlineData("t0,t5", "0,5", "b0,b5", "0,5", "t0,b0,t5,b5")]
    [InlineData("t9", "9", "b1,b2", "1,2", "b1,b2,t9")]
    [InlineData("", "", "", "", "")]
    public void OrderedMerge_VisitsBothSidesInZOrderWithTablesFirstOnATie(
        string tables,
        string tableOrders,
        string boxes,
        string boxOrders,
        string expected)
    {
        static ImmutableArray<string> Names(string value)
            => value.Length == 0 ? [] : [.. value.Split(',')];

        static Func<string, int> Order(string names, string orders)
        {
            var map = Names(names)
                .Zip(Names(orders).Select(int.Parse), (name, order) => (name, order))
                .ToDictionary(pair => pair.name, pair => pair.order, StringComparer.Ordinal);
            return name => map[name];
        }

        var visited = Merge(
            Names(tables),
            Order(tables, tableOrders),
            Names(boxes),
            Order(boxes, boxOrders));

        Assert.Equal(Names(expected), visited);
    }
}
