#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TextExtractionTests
{
    private static PortableDocument Reload(PortableDocument document)
    {
        using var buffer = new MemoryStream(document.ToArray());
        return PortableDocument.LoadFromStream(buffer);
    }

    [Fact]
    public void SimpleBase14_RoundTripsAuthoredString()
    {
        var text = "Hello World";
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent(text, 72, 700));

        var reloaded = Reload(document);
        Assert.Equal(text, reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void SimpleBase14_PreservesLatin1Characters()
    {
        var text = "Café £5 costs €10";
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent(text, 72, 700));

        var reloaded = Reload(document);
        Assert.Equal(text, reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void EmptyPage_ExtractsEmptyString()
    {
        var document = new PortableDocument();
        document.Pages.Add();

        var reloaded = Reload(document);
        Assert.Equal(string.Empty, reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void ReadingOrder_SortsTopToBottomThenLeftToRight()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();

        page.Content.Add(new TextContent("Right", 300, 500));
        page.Content.Add(new TextContent("Charlie", 72, 600));
        page.Content.Add(new TextContent("Alpha", 72, 700));
        page.Content.Add(new TextContent("Left", 72, 500));
        page.Content.Add(new TextContent("Bravo", 72, 650));

        var text = Reload(document).Pages[0].ExtractText();

        var alpha = text.IndexOf("Alpha", StringComparison.Ordinal);
        var bravo = text.IndexOf("Bravo", StringComparison.Ordinal);
        var charlie = text.IndexOf("Charlie", StringComparison.Ordinal);
        var left = text.IndexOf("Left", StringComparison.Ordinal);
        var right = text.IndexOf("Right", StringComparison.Ordinal);

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
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent(text, 72, 700));

        var reloaded = Reload(document);
        Assert.Equal(text, reloaded.ExtractText());
    }

    [Fact]
    public void DocumentExtractText_ConcatenatesPagesInOrder()
    {
        var document = new PortableDocument();
        document.Pages.Add().Content.Add(new TextContent("PageOneBody", 72, 700));
        document.Pages.Add().Content.Add(new TextContent("PageTwoBody", 72, 700));

        var text = Reload(document).ExtractText();

        var one = text.IndexOf("PageOneBody", StringComparison.Ordinal);
        var two = text.IndexOf("PageTwoBody", StringComparison.Ordinal);

        Assert.True(one >= 0, "page one text present");
        Assert.True(two >= 0, "page two text present");
        Assert.True(one < two, "page one precedes page two");
    }
}
