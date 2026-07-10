#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// End-to-end extraction over the merged authoring/reload path (C3 emit -> C4a
// materialize). Pins string Page.ExtractText() and string Document.ExtractText().
public class TextExtractionTests
{
    private static Document Reload(Document document)
    {
        using var buffer = new System.IO.MemoryStream(document.ToArray());
        return Document.LoadFromStream(buffer);
    }

    [Fact]
    public void SimpleBase14_RoundTripsAuthoredString()
    {
        var text = "Hello World";
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent(text, 72, 700));

        var reloaded = Reload(document);
        Assert.Equal(text, reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void SimpleBase14_PreservesLatin1Characters()
    {
        // e-acute (U+00E9), sterling (U+00A3) and euro (U+20AC) are all WinAnsi
        // codes distinct from their UTF-8 byte sequences, so a wrong decode shows.
        var text = "Café £5 costs €10";
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent(text, 72, 700));

        var reloaded = Reload(document);
        Assert.Equal(text, reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void EmptyPage_ExtractsEmptyString()
    {
        var document = new Document();
        document.Pages.Add();

        var reloaded = Reload(document);
        Assert.Equal(string.Empty, reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void ReadingOrder_SortsTopToBottomThenLeftToRight()
    {
        var document = new Document();
        var page = document.Pages.Add();

        // Added out of visual order; extraction must reorder by descending Y then
        // ascending X. "Left" and "Right" share a baseline to exercise the X tiebreak.
        page.Content.Add(new TextContent("Right", 300, 500));
        page.Content.Add(new TextContent("Charlie", 72, 600));
        page.Content.Add(new TextContent("Alpha", 72, 700));
        page.Content.Add(new TextContent("Left", 72, 500));
        page.Content.Add(new TextContent("Bravo", 72, 650));

        var text = Reload(document).Pages[0].ExtractText();

        var alpha = text.IndexOf("Alpha", System.StringComparison.Ordinal);
        var bravo = text.IndexOf("Bravo", System.StringComparison.Ordinal);
        var charlie = text.IndexOf("Charlie", System.StringComparison.Ordinal);
        var left = text.IndexOf("Left", System.StringComparison.Ordinal);
        var right = text.IndexOf("Right", System.StringComparison.Ordinal);

        Assert.True(alpha >= 0 && bravo >= 0 && charlie >= 0 && left >= 0 && right >= 0);
        Assert.True(alpha < bravo, "Alpha (y=700) before Bravo (y=650)");
        Assert.True(bravo < charlie, "Bravo (y=650) before Charlie (y=600)");
        Assert.True(charlie < left, "Charlie (y=600) before Left (y=500)");
        Assert.True(left < right, "Left (x=72) before Right (x=300) on the same baseline");
    }

    [Fact]
    public void DocumentExtractText_SinglePageEqualsPageText()
    {
        var text = "Single page body";
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent(text, 72, 700));

        var reloaded = Reload(document);
        Assert.Equal(text, reloaded.ExtractText());
    }

    [Fact]
    public void DocumentExtractText_ConcatenatesPagesInOrder()
    {
        var document = new Document();
        document.Pages.Add().Content.Add(new TextContent("PageOneBody", 72, 700));
        document.Pages.Add().Content.Add(new TextContent("PageTwoBody", 72, 700));

        var text = Reload(document).ExtractText();

        var one = text.IndexOf("PageOneBody", System.StringComparison.Ordinal);
        var two = text.IndexOf("PageTwoBody", System.StringComparison.Ordinal);

        Assert.True(one >= 0, "page one text present");
        Assert.True(two >= 0, "page two text present");
        Assert.True(one < two, "page one precedes page two");
    }
}
