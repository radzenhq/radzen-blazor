using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PageOperationEditingTests
{
    [Fact]
    public void MediaAndCropBoxes_RoundTripAfterEditing()
    {
        var document = Reload(Create("PAGE"));
        document.Pages[0].MediaBox = PdfRect.FromSize(10, 20, 300, 400);
        document.Pages[0].CropBox = PdfRect.FromSize(25, 35, 250, 325);

        var reloaded = Reload(document);

        Assert.Equal(PdfRect.FromSize(10, 20, 300, 400), reloaded.Pages[0].MediaBox);
        Assert.Equal(PdfRect.FromSize(25, 35, 250, 325), reloaded.Pages[0].CropBox);
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
    public void ExtractPages_DroppedLinkAndOutOfRangePageAreAbsentFromSavedObjects()
    {
        var source = Reload(Create("ONE", "TWO", "THREE"));
        source.Pages[0].Annotations.Add(new LinkAnnotation(PdfRect.FromSize(10, 10, 20, 20)) { TargetPageIndex = 2 });
        source.Pages[0].Annotations.Add(new LinkAnnotation(PdfRect.FromSize(40, 10, 20, 20)) { TargetPageIndex = 0 });

        var bytes = source.Pages.ExtractPages(0..1).ToArray();
        var extracted = Document.LoadFromStream(new MemoryStream(bytes));

        var link = Assert.IsType<LinkAnnotation>(Assert.Single(extracted.Pages[0].Annotations));
        Assert.Equal(0, link.TargetPageIndex);
        var text = Encoding.Latin1.GetString(bytes);
        Assert.Equal(1, Regex.Matches(text, @"/Subtype\s*/Link\b").Count);
        Assert.Equal(1, Regex.Matches(text, @"/Type\s*/Page\b").Count);
    }

    [Fact]
    public void ExtractPages_ResolvedNamedDestinationSurvivesWhenTargetIsRetainedAndDropsWhenExcluded()
    {
        var source = Document.LoadFromStream(new MemoryStream(NamedDestinationPreservationTests.Source()));

        var retained = Reload(source.Pages.ExtractPages(..));
        var excluded = Reload(source.Pages.ExtractPages(0..1));

        var link = Assert.IsType<LinkAnnotation>(Assert.Single(retained.Pages[0].Annotations));
        Assert.Null(link.Destination);
        Assert.Equal(1, link.TargetPageIndex);
        Assert.Empty(excluded.Pages[0].Annotations);
    }

    [Fact]
    public void Split_DropsCrossPageLinkAndRebasesRetainedPageLink()
    {
        var source = Reload(Create("ONE", "TWO", "THREE"));
        source.Pages[1].Annotations.Add(new LinkAnnotation(PdfRect.FromSize(10, 10, 20, 20)) { TargetPageIndex = 0 });
        source.Pages[1].Annotations.Add(new LinkAnnotation(PdfRect.FromSize(40, 10, 20, 20)) { TargetPageIndex = 1 });

        var split = source.Pages.Split(1);
        var first = Reload(split[0]);
        var second = Reload(split[1]);

        Assert.Empty(first.Pages[0].Annotations);
        var link = Assert.IsType<LinkAnnotation>(Assert.Single(second.Pages[0].Annotations));
        Assert.Equal(0, link.TargetPageIndex);
    }

    [Fact]
    public void ImportPages_RebasesLinksForEmptyAndNonEmptyTargets()
    {
        var source = new Document();
        var sourcePage = source.Pages.Add();
        sourcePage.Annotations.Add(new LinkAnnotation(PdfRect.FromSize(10, 10, 20, 20)) { TargetPageIndex = 1 });
        sourcePage.Annotations.Add(new LinkAnnotation(PdfRect.FromSize(40, 10, 20, 20)) { Uri = new Uri("https://example.com/") });
        source.Pages.Add();

        var empty = new Document();
        empty.ImportPages(source, ..);
        var nonEmpty = new Document();
        nonEmpty.Pages.Add();
        nonEmpty.Pages.Add();
        nonEmpty.ImportPages(source, ..);

        Assert.Equal(1, Assert.IsType<LinkAnnotation>(empty.Pages[0].Annotations[0]).TargetPageIndex);
        Assert.Equal(3, Assert.IsType<LinkAnnotation>(nonEmpty.Pages[2].Annotations[0]).TargetPageIndex);
        Assert.Equal("https://example.com/", Assert.IsType<LinkAnnotation>(nonEmpty.Pages[2].Annotations[1]).Uri?.AbsoluteUri);
        var reloaded = Reload(nonEmpty);
        Assert.Equal(3, Assert.IsType<LinkAnnotation>(reloaded.Pages[2].Annotations[0]).TargetPageIndex);
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
