using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PageOperationEditingTests
{
    [Fact]
    public void MediaAndCropBoxes_RoundTripAfterEditing()
    {
        var document = Reload(Create("PAGE"));
        document.Pages[0].MediaBox = new Rect(10, 20, 300, 400);
        document.Pages[0].CropBox = new Rect(25, 35, 250, 325);

        var reloaded = Reload(document);

        Assert.Equal(new Rect(10, 20, 300, 400), reloaded.Pages[0].MediaBox);
        Assert.Equal(new Rect(25, 35, 250, 325), reloaded.Pages[0].CropBox);
        Assert.Equal(300, reloaded.Pages[0].Width.Point);
        Assert.Equal(400, reloaded.Pages[0].Height.Point);
        Assert.Contains("PAGE", reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void Move_ReordersLoadedPagesAfterRoundTrip()
    {
        var document = Reload(Create("ONE", "TWO", "THREE"));

        document.Pages.Move(0, 2);

        Assert.Equal(["TWO", "THREE", "ONE"], PageTexts(Reload(document)));
    }

    [Fact]
    public void RemoveRange_DeletesSelectedLoadedPages()
    {
        var document = Reload(Create("ONE", "TWO", "THREE", "FOUR"));

        document.Pages.RemoveRange(1, 2);

        Assert.Equal(["ONE", "FOUR"], PageTexts(Reload(document)));
    }

    [Fact]
    public void ExtractAndSplit_ClonePageCountsAndContent()
    {
        var source = Reload(Create("ONE", "TWO", "THREE", "FOUR", "FIVE"));

        var extracted = source.Pages.ExtractPages(1..4);
        var split = source.Pages.Split(2, 4);

        Assert.Equal(["TWO", "THREE", "FOUR"], PageTexts(Reload(extracted)));
        Assert.Equal(3, split.Count);
        Assert.Equal(["ONE", "TWO"], PageTexts(Reload(split[0])));
        Assert.Equal(["THREE", "FOUR"], PageTexts(Reload(split[1])));
        Assert.Equal(["FIVE"], PageTexts(Reload(split[2])));
    }

    [Fact]
    public void ImportPageAndRange_CopySelectedLoadedContentAndResources()
    {
        var target = Reload(Create("TARGET"));
        var source = Reload(Create("SOURCE ONE", "SOURCE TWO", "SOURCE THREE"));

        target.ImportPage(source, 1);
        target.ImportPages(source, 0..1);

        Assert.Equal(["TARGET", "SOURCE TWO", "SOURCE ONE"], PageTexts(Reload(target)));
    }

    [Fact]
    public void Merge_CombinesDocumentsInOrder()
    {
        var first = Reload(Create("ONE", "TWO"));
        var second = Create("THREE");

        var merged = Document.Merge(first, second);

        Assert.Equal(["ONE", "TWO", "THREE"], PageTexts(Reload(merged)));
    }

    [Fact]
    public void AddWatermark_StampsEveryLoadedPage()
    {
        var document = Reload(Create("ONE", "TWO", "THREE"));

        document.AddWatermark(new Watermark { Text = "DRAFT", Opacity = 0.25, Rotation = 30 });

        var reloaded = Reload(document);
        Assert.All(reloaded.Pages, page => Assert.Contains("DRAFT", page.ExtractText()));
    }

    private static Document Create(params string[] texts)
    {
        var document = new Document();
        foreach (var text in texts)
        {
            var page = document.Pages.Add();
            page.Content.Add(new TextContent(text, Unit.FromPoint(72), Unit.FromPoint(700)));
        }

        return document;
    }

    private static Document Reload(Document document)
        => Document.LoadFromStream(new MemoryStream(document.ToArray()));

    private static string[] PageTexts(Document document)
        => document.Pages.Select(page => page.ExtractText()).ToArray();
}
