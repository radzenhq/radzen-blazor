#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ConcurrentSaveTests
{
    private const string Heading = "Heading";

    private static DocumentBuilder Author()
    {
        var builder = new DocumentBuilder();

        var heading = builder.Styles.Add(Heading);
        heading.Alignment = HorizontalAlignment.Center;
        heading.Font.Size = 18;
        heading.Font.Bold = true;

        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(2000));
        section.Margin = Unit.FromPoint(40);

        for (var i = 0; i < 30; i++)
        {
            var paragraph = section.Blocks.AddParagraph("Centered heading number " + i);
            paragraph.StyleName = Heading;
        }

        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Size = 14;
        list.AddItem("first list item");
        list.AddItem("second list item");

        return builder;
    }

    [Fact]
    public void ConcurrentSavesProduceByteIdenticalOutput()
    {
        var builder = Author();
        var reference = builder.ToArray();

        const int threads = 8;
        const int rounds = 60;
        using var start = new Barrier(threads);
        var mismatches = new ConcurrentBag<int>();

        var tasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            start.SignalAndWait();
            for (var round = 0; round < rounds; round++)
            {
                var output = builder.ToArray();
                if (!output.AsSpan().SequenceEqual(reference))
                {
                    mismatches.Add(round);
                }
            }
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.True(mismatches.IsEmpty, $"{mismatches.Count} of {threads * rounds} concurrent saves diverged from the single-threaded reference");
    }

    [Fact]
    public void SingleSaveRendersStyleCenterAlignmentAndStyledAndListFonts()
    {
        var referenceBuilder = new DocumentBuilder();
        var referenceSection = referenceBuilder.Sections.Add();
        referenceSection.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(2000));
        referenceSection.Margin = Unit.FromPoint(40);
        var centered = referenceSection.Blocks.AddParagraph("Centered heading number 0");
        centered.Alignment = HorizontalAlignment.Center;
        centered.Font.Size = 18;
        centered.Font.Bold = true;
        var expectedX = CascadeTestSupport.TdPositions(CascadeTestSupport.FirstPageContent(referenceBuilder))[0].X;

        var builder = Author();
        var content = CascadeTestSupport.FirstPageContent(builder);

        var positions = CascadeTestSupport.TdPositions(content);
        Assert.Contains(positions, p => Math.Abs(p.X - expectedX) < 1);
        Assert.Contains(positions, p => Math.Abs(p.X - 40) < 1);

        var sizes = CascadeTestSupport.TfSizes(content);
        Assert.Contains(18.0, sizes);
        Assert.Contains(14.0, sizes);
    }
}
