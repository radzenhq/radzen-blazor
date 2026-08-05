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

using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Codes;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class ModelReentrancyTests
{
    private const string Heading = "Heading";
    private const string CellStyle = "CellHeading";

    private static byte[] RenderAuthored((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static Document Author()
    {
        var document = new Document();

        var heading = document.Styles.Add(Heading);
        heading.Alignment = HorizontalAlignment.Center;
        heading.Font.Size = 18;
        heading.Font.Bold = true;

        var cellHeading = document.Styles.Add(CellStyle);
        cellHeading.Alignment = HorizontalAlignment.Right;
        cellHeading.Font.Size = 12;

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(2000));
        section.Margins.SetAll(Unit.FromPoint(40));

        for (var i = 0; i < 10; i++)
        {
            section.Blocks.Add(new Paragraph("Centered heading number " + i)).StyleName = Heading;
        }

        var list = section.Blocks.Add(new ListBlock { Style = ListStyle.Bullet });
        list.Font.Size = 14;
        list.Items.Add("first list item");
        var second = list.Items.Add("second list item");
        var nested = second.NestedList = new ListBlock { Style = ListStyle.Number };
        nested.Items.Add("nested one");
        nested.Items.Add("nested two");

        var table = section.Blocks.Add(new Table());
        table.Columns.Add();
        table.Columns.Add();
        for (var r = 0; r < 3; r++)
        {
            var row = table.Rows.Add();
            var styled = row.Cells[0].Blocks.Add(new Paragraph("row " + r + " left"));
            styled.StyleName = CellStyle;
            row.Cells[1].Blocks.Add(new Paragraph("row " + r + " right"));
        }

        var barcode = section.Blocks.Add(new Barcode(BarcodeType.Code128, "REENTRANT-1", Unit.FromPoint(160), Unit.FromPoint(40)) { ShowText = true });
        barcode.Font.Size = 9;

        return document;
    }

    private static (Document Document, DocumentRenderer Renderer) AuthorTaggedList()
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Tagged";
        document.Language = "en-US";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(800));
        section.Margins.SetAll(Unit.FromPoint(40));

        var list = section.Blocks.Add(new ListBlock { Style = ListStyle.Bullet });
        list.Font.Family = BuildTestSupport.Latin;
        list.Items.Add("first tagged item");
        var second = list.Items.Add("second tagged item");
        var nested = second.NestedList = new ListBlock { Style = ListStyle.Number };
        nested.Font.Family = BuildTestSupport.Latin;
        nested.Items.Add("nested tagged one");
        nested.Items.Add("nested tagged two");

        return (document, builderRenderer);
    }

    [Fact]
    public void GeneratingTwiceIsByteIdentical()
    {
        var document = Author();
        var builderRenderer = new DocumentRenderer();
        var first = builderRenderer.ToArray(document);
        var second = builderRenderer.ToArray(document);
        var third = builderRenderer.ToArray(document);

        Assert.True(first.AsSpan().SequenceEqual(second), "second generation diverged from the first");
        Assert.True(first.AsSpan().SequenceEqual(third), "third generation diverged from the first");
    }

    [Fact]
    public void GeneratingLeavesNoOutputAffectingScratch()
    {
        var used = Author();
        _ = new DocumentRenderer().ToArray(used);
        var afterUse = new DocumentRenderer().ToArray(used);

        var fresh = Author();
        var freshFirst = new DocumentRenderer().ToArray(fresh);

        Assert.True(afterUse.AsSpan().SequenceEqual(freshFirst),
            "a generated document diverged from a fresh equivalent - generation left scratch behind");
    }

    [Fact]
    public void ConcurrentGenerationFromTwoThreadsIsByteIdentical()
    {
        var document = Author();
        var builderRenderer = new DocumentRenderer();
        var reference = builderRenderer.ToArray(document);

        const int threads = 2;
        const int rounds = 40;
        using var start = new Barrier(threads);
        var mismatches = new ConcurrentBag<int>();

        var tasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            start.SignalAndWait();
            for (var round = 0; round < rounds; round++)
            {
                if (!builderRenderer.ToArray(document).AsSpan().SequenceEqual(reference))
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
        var document = Author();
        var builderRenderer = new DocumentRenderer();

        var section = document.Sections[0];
        var headings = section.Blocks.OfType<Paragraph>().ToList();
        var alignmentsBefore = headings.Select(p => p.Alignment).ToList();
        var styleNamesBefore = headings.Select(p => p.StyleName).ToList();

        _ = builderRenderer.ToArray(document);

        var alignmentsAfter = headings.Select(p => p.Alignment).ToList();
        var styleNamesAfter = headings.Select(p => p.StyleName).ToList();

        Assert.All(alignmentsAfter, Assert.Null);
        Assert.Equal(alignmentsBefore, alignmentsAfter);
        Assert.Equal(styleNamesBefore, styleNamesAfter);
    }

    [Fact]
    public void TaggedListGeneratingTwiceIsByteIdentical()
    {
        var (document, builderRenderer) = AuthorTaggedList();
        var first = builderRenderer.ToArray(document);
        var second = builderRenderer.ToArray(document);

        Assert.True(first.AsSpan().SequenceEqual(second), "tagged-list second generation diverged");

        var fresh = RenderAuthored(AuthorTaggedList());
        Assert.True(first.AsSpan().SequenceEqual(fresh), "tagged list left scratch behind after generation");
    }
}
