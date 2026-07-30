#nullable enable

using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendPendingContentTests
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

    private static Document Reload(Document document)
        => Document.LoadFromStream(new MemoryStream(document.ToArray()));

    private static string[] PageTexts(Document document)
        => document.Pages.Select(page => page.ExtractText()).ToArray();

    [Fact]
    public void Append_CarriesWatermarkQueuedOnTheSourcePage()
    {
        var source = Loaded(1);
        source.AddWatermark("DRAFT");

        var target = new Document();
        target.Append(source);

        Assert.Contains("DRAFT", Reload(target).Pages[0].ExtractText());
    }

    [Fact]
    public void Append_CarriesEditsMadeToTheSourcePage()
    {
        var source = Loaded(2);
        source.Pages[1].Content.Clear();
        source.Pages[1].Content.Add(new TextContent("EDITED", Unit.FromPoint(72), Unit.FromPoint(700)));

        var target = new Document();
        target.Append(source);

        Assert.Equal(["PAGE 0", "EDITED"], PageTexts(Reload(target)));
    }

    [Fact]
    public void Append_LeavesTheSourceDocumentUnchanged()
    {
        var source = Loaded(1);
        source.AddWatermark("DRAFT");
        var before = source.ToArray();

        new Document().Append(source);

        Assert.Equal(before, source.ToArray());
    }

    [Fact]
    public void Append_OfAnIntactLoadedPageKeepsItsBytes()
    {
        var source = Loaded(1);
        var expected = source.Pages[0].GetContent();

        var target = new Document();
        target.Append(source);

        Assert.Equal(expected, target.Pages[0].GetContent());
    }

    [Fact]
    public void Append_OfABuiltDocumentCarriesItsContent()
    {
        var source = new Document();
        source.Pages.Add().Content.Add(new TextContent("BUILT", Unit.FromPoint(72), Unit.FromPoint(700)));

        var target = new Document();
        target.Append(source);

        Assert.Equal(["BUILT"], PageTexts(Reload(target)));
    }
}
