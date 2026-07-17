#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ModelReentrancyTests
{
    private const string Heading = "Heading";
    private const string CellStyle = "CellHeading";

    private static DocumentBuilder Author()
    {
        var builder = new DocumentBuilder();

        var heading = builder.Styles.Add(Heading);
        heading.Alignment = HorizontalAlignment.Center;
        heading.Font.Size = 18;
        heading.Font.Bold = true;

        var cellHeading = builder.Styles.Add(CellStyle);
        cellHeading.Alignment = HorizontalAlignment.Right;
        cellHeading.Font.Size = 12;

        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(2000));
        section.Margin = Unit.FromPoint(40);

        for (var i = 0; i < 10; i++)
        {
            section.Blocks.AddParagraph("Centered heading number " + i).StyleName = Heading;
        }

        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Size = 14;
        list.AddItem("first list item");
        var second = list.AddItem("second list item");
        var nested = second.AddList(ListStyle.Number);
        nested.AddItem("nested one");
        nested.AddItem("nested two");

        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        for (var r = 0; r < 3; r++)
        {
            var row = table.Rows.Add();
            var styled = row.Cells[0].Blocks.AddParagraph("row " + r + " left");
            styled.StyleName = CellStyle;
            row.Cells[1].Blocks.AddParagraph("row " + r + " right");
        }

        var barcode = section.Blocks.AddBarcode(BarcodeType.Code128, "REENTRANT-1", Unit.FromPoint(160), Unit.FromPoint(40), showText: true);
        barcode.Font.Size = 9;

        return builder;
    }

    private static DocumentBuilder AuthorTaggedList()
    {
        var builder = new DocumentBuilder { PdfUA = true };
        builder.Info.Title = "Tagged";
        builder.Language = "en-US";
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(800));
        section.Margin = Unit.FromPoint(40);

        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Name = BuildTestSupport.Latin;
        list.AddItem("first tagged item");
        var second = list.AddItem("second tagged item");
        var nested = second.AddList(ListStyle.Number);
        nested.Font.Name = BuildTestSupport.Latin;
        nested.AddItem("nested tagged one");
        nested.AddItem("nested tagged two");

        return builder;
    }

    [Fact]
    public void GeneratingTwiceIsByteIdentical()
    {
        var builder = Author();
        var first = builder.ToArray();
        var second = builder.ToArray();
        var third = builder.ToArray();

        Assert.True(first.AsSpan().SequenceEqual(second), "second generation diverged from the first");
        Assert.True(first.AsSpan().SequenceEqual(third), "third generation diverged from the first");
    }

    [Fact]
    public void GeneratingLeavesNoOutputAffectingScratch()
    {
        var used = Author();
        _ = used.ToArray();
        var afterUse = used.ToArray();

        var fresh = Author();
        var freshFirst = fresh.ToArray();

        Assert.True(afterUse.AsSpan().SequenceEqual(freshFirst),
            "a generated builder diverged from a fresh equivalent - generation left scratch behind");
    }

    [Fact]
    public void ConcurrentGenerationFromTwoThreadsIsByteIdentical()
    {
        var builder = Author();
        var reference = builder.ToArray();

        const int threads = 2;
        const int rounds = 40;
        using var start = new Barrier(threads);
        var mismatches = new ConcurrentBag<int>();

        var tasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            start.SignalAndWait();
            for (var round = 0; round < rounds; round++)
            {
                if (!builder.ToArray().AsSpan().SequenceEqual(reference))
                {
                    mismatches.Add(round);
                }
            }
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.True(mismatches.IsEmpty, $"{mismatches.Count} concurrent generations diverged from the reference");
    }

    [Fact]
    public void PublicModelStateIsUnchangedAfterGeneration()
    {
        var builder = Author();

        var section = builder.Sections[0];
        var headings = section.Blocks.OfType<Paragraph>().ToList();
        var alignmentsBefore = headings.Select(p => p.Alignment).ToList();
        var styleNamesBefore = headings.Select(p => p.StyleName).ToList();

        _ = builder.ToArray();

        var alignmentsAfter = headings.Select(p => p.Alignment).ToList();
        var styleNamesAfter = headings.Select(p => p.StyleName).ToList();

        Assert.All(alignmentsAfter, a => Assert.Equal(HorizontalAlignment.Left, a));
        Assert.Equal(alignmentsBefore, alignmentsAfter);
        Assert.Equal(styleNamesBefore, styleNamesAfter);
    }

    [Fact]
    public void TaggedListGeneratingTwiceIsByteIdentical()
    {
        var builder = AuthorTaggedList();
        var first = builder.ToArray();
        var second = builder.ToArray();

        Assert.True(first.AsSpan().SequenceEqual(second), "tagged-list second generation diverged");

        var fresh = AuthorTaggedList().ToArray();
        Assert.True(first.AsSpan().SequenceEqual(fresh), "tagged list left scratch behind after generation");
    }

    [Theory]
    [InlineData(typeof(Run))]
    [InlineData(typeof(Paragraph))]
    [InlineData(typeof(Barcode))]
    [InlineData(typeof(ListItem))]
    public void ModelTypesHaveNoPerSaveScratchProperty(Type modelType)
    {
        string[] forbidden =
        [
            "EffectiveFont", "ResolvedFont", "LabelElement", "BodyElement",
            "ListLabelElement", "ListBodyElement",
        ];

        var members = modelType
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in forbidden)
        {
            Assert.False(members.Contains(name),
                $"{modelType.Name} still exposes per-save scratch member '{name}'");
        }
    }
}
