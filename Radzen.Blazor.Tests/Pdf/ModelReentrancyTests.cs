#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The authoring model (DocumentBuilder and everything it owns) must be a pure downward
// layer: generating a document reads the model but never mutates it, so one shared builder
// can be generated repeatedly and concurrently with byte-identical output. Style resolution
// used to write the resolved alignment back onto the paragraph as per-save scratch; it now
// lives only in the generator-owned StyleResolution, which these tests pin.
public class ModelReentrancyTests
{
    private const string Heading = "Heading";
    private const string CellStyle = "CellHeading";

    // Exercises the paths that resolve style-derived alignment: body paragraphs with a
    // centered named style, a bullet list with a nested list, and a table whose cells hold
    // right-aligned styled paragraphs (the cell path that formerly read model scratch).
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
        // A builder that has already been generated must produce exactly what a freshly
        // authored, never-generated equivalent produces: any per-save scratch left on the
        // model that fed back into a later save would show up as a divergence here.
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

        // A style-centered paragraph keeps its own Alignment at the default - the resolved
        // center lives in the generator, never written onto the model.
        Assert.All(alignmentsAfter, a => Assert.Equal(HorizontalAlignment.Left, a));
        Assert.Equal(alignmentsBefore, alignmentsAfter);
        Assert.Equal(styleNamesBefore, styleNamesAfter);
    }
}
