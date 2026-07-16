#nullable enable

using System;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Importing one page out of a loaded document must not serialize and re-parse the whole
// source: importing pages one at a time from an N-page document cost N full round trips
// (finding #125).
public class ImportPageCostTests
{
    private static Document Loaded(int pages)
    {
        var document = new Document();
        for (var i = 0; i < pages; i++)
        {
            var page = document.Pages.Add();
            page.Content.Add(new TextContent($"PAGE {i}", Unit.FromPoint(72), Unit.FromPoint(700)));
        }

        return Document.LoadFromStream(new MemoryStream(document.ToArray()));
    }

    private static long ImportBytes(int sourcePages)
    {
        var source = Loaded(sourcePages);

        // Warm up the reader caches and the JIT so only the import itself is measured.
        new Document().ImportPage(source, 0);

        var before = GC.GetAllocatedBytesForCurrentThread();
        new Document().ImportPage(source, 0);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void ImportPage_AllocationDoesNotScaleWithSourcePageCount()
    {
        var small = ImportBytes(20);
        var large = ImportBytes(160);

        // 8x the source pages must cost the same single page import; a whole-document
        // serialize plus re-parse per call costs ~8x.
        Assert.InRange((double)large / small, 0, 2.0);
    }

    [Fact]
    public void ImportPages_OneAtATimeMatchesRangeImport()
    {
        var source = Loaded(4);
        var target = new Document();
        for (var i = 0; i < source.Pages.Count; i++)
        {
            target.ImportPage(source, i);
        }

        var range = new Document();
        range.ImportPages(source, ..);

        Assert.Equal(PageTexts(Reload(range)), PageTexts(Reload(target)));
        Assert.Equal(["PAGE 0", "PAGE 1", "PAGE 2", "PAGE 3"], PageTexts(Reload(target)));
    }

    [Fact]
    public void ImportPage_CarriesEditsMadeToTheSourcePage()
    {
        var source = Loaded(2);
        source.Pages[1].Content.Clear();
        source.Pages[1].Content.Add(new TextContent("EDITED", Unit.FromPoint(72), Unit.FromPoint(700)));

        var target = new Document();
        target.ImportPage(source, 1);

        Assert.Equal(["EDITED"], PageTexts(Reload(target)));
    }

    [Fact]
    public void ImportPage_CarriesWatermarkQueuedOnTheSourcePage()
    {
        var source = Loaded(2);
        source.AddWatermark("DRAFT");

        var target = new Document();
        target.ImportPage(source, 0);

        Assert.Contains("DRAFT", Reload(target).Pages[0].ExtractText());
    }

    [Fact]
    public void ImportPage_CarriesRotationSetOnTheSourcePage()
    {
        var source = Loaded(1);
        source.Pages[0].Rotate = 90;

        var target = new Document();
        target.ImportPage(source, 0);

        Assert.Equal(90, Reload(target).Pages[0].Rotate);
    }

    private static Document Reload(Document document)
        => Document.LoadFromStream(new MemoryStream(document.ToArray()));

    private static string[] PageTexts(Document document)
        => document.Pages.Select(page => page.ExtractText()).ToArray();
}
