#nullable enable

using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

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
