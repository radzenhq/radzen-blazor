#nullable enable

using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class EmptiedPageContentTests
{
    private static Document LoadedSinglePathDocument()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("10 10 100 100 re f\n"));
        return InterpreterTestSupport.Load(document.ToArray());
    }

    private static string SavedPageContent(Document document, int pageIndex = 0)
        => Encoding.ASCII.GetString(InterpreterTestSupport.PageContentBytes(document.ToArray(), pageIndex));

    [Fact]
    public void Redact_RemovesOnlyElementOnLoadedPage_ContentIsGone()
    {
        var loaded = LoadedSinglePathDocument();

        loaded.Pages[0].Redact([PdfRect.FromSize(0, 0, 612, 792)]);

        Assert.DoesNotContain("re", SavedPageContent(loaded));
    }

    [Fact]
    public void Redact_RemovesOnlyElementOnLoadedPage_ContentGoneAfterReload()
    {
        var loaded = LoadedSinglePathDocument();

        loaded.Pages[0].Redact([PdfRect.FromSize(0, 0, 612, 792)]);
        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());

        Assert.Empty(reloaded.Pages[0].Content);
        Assert.DoesNotContain("re", SavedPageContent(reloaded));
    }

    [Fact]
    public void ContentRemove_RemovesOnlyElementOnLoadedPage_ContentIsGone()
    {
        var loaded = LoadedSinglePathDocument();

        var content = loaded.Pages[0].Content;
        Assert.Single(content);
        content.RemoveAt(0);

        Assert.DoesNotContain("re", SavedPageContent(loaded));
    }

    [Fact]
    public void ContentClear_RemovesEveryElementOnLoadedTextPage_TextIsGone()
    {
        var document = new Document();
        document.Pages.Add().Content.Add(new TextContent("Secret", 72, 700) { Font = new Font { Size = 12 } });
        var loaded = InterpreterTestSupport.Load(document.ToArray());

        loaded.Pages[0].Content.Clear();

        Assert.DoesNotContain("Secret", SavedPageContent(loaded));
    }

    [Fact]
    public void RedactText_RemovesOnlyTextOnLoadedPage_TextIsGone()
    {
        var document = new Document();
        document.Pages.Add().Content.Add(new TextContent("Secret", 72, 700) { Font = new Font { Size = 12 } });
        var loaded = InterpreterTestSupport.Load(document.ToArray());

        Assert.Equal(1, loaded.Pages[0].RedactText("Secret"));

        Assert.DoesNotContain("Secret", SavedPageContent(loaded));
    }

    [Fact]
    public void NeverMaterializedLoadedPage_ReusesRawBytesVerbatim()
    {
        var original = LoadedSinglePathDocument().ToArray();

        var loaded = InterpreterTestSupport.Load(original);
        var resaved = loaded.ToArray();

        Assert.Equal(
            InterpreterTestSupport.PageContentBytes(original, 0),
            InterpreterTestSupport.PageContentBytes(resaved, 0));
    }

    [Fact]
    public void MaterializedUnmodifiedLoadedPage_ReusesRawBytesVerbatim()
    {
        var original = LoadedSinglePathDocument().ToArray();

        var loaded = InterpreterTestSupport.Load(original);
        Assert.Single(loaded.Pages[0].Content);
        var resaved = loaded.ToArray();

        Assert.Equal(
            InterpreterTestSupport.PageContentBytes(original, 0),
            InterpreterTestSupport.PageContentBytes(resaved, 0));
    }

    [Fact]
    public void EmptyContentStreamPage_NeverMaterialized_StaysByteIdentical()
    {
        var document = new Document();
        document.Pages.Add().SetContent([]);
        var original = document.ToArray();

        var resaved = InterpreterTestSupport.Load(original).ToArray();

        Assert.Equal(
            InterpreterTestSupport.PageContentBytes(original, 0),
            InterpreterTestSupport.PageContentBytes(resaved, 0));
    }

    [Fact]
    public void EmptyContentStreamPage_Materialized_StaysByteIdentical()
    {
        var document = new Document();
        document.Pages.Add().SetContent([]);
        var original = document.ToArray();

        var loaded = InterpreterTestSupport.Load(original);
        Assert.Empty(loaded.Pages[0].Content);
        var resaved = loaded.ToArray();

        Assert.Equal(
            InterpreterTestSupport.PageContentBytes(original, 0),
            InterpreterTestSupport.PageContentBytes(resaved, 0));
    }

    [Fact]
    public void AuthoredPageWithNoContent_SavesSensibly()
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4);

        var saved = document.ToArray();

        var reloaded = InterpreterTestSupport.Load(saved);
        Assert.Single(reloaded.Pages);
        Assert.Empty(reloaded.Pages[0].Content);
    }

    [Fact]
    public void AuthoredPageWithNoContent_ContentReadBeforeSave_SavesSensibly()
    {
        var document = new Document();
        var page = document.Pages.Add(PageSizes.A4);
        Assert.Empty(page.Content);

        var reloaded = InterpreterTestSupport.Load(document.ToArray());

        Assert.Single(reloaded.Pages);
        Assert.Empty(reloaded.Pages[0].Content);
    }

    [Fact]
    public void EmptiedLoadedPage_KeepsOtherPagesUntouched()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("10 10 100 100 re f\n"));
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("20 20 50 50 re f\n"));
        var original = document.ToArray();

        var loaded = InterpreterTestSupport.Load(original);
        loaded.Pages[0].Content.Clear();
        var resaved = loaded.ToArray();

        Assert.DoesNotContain("re", Encoding.ASCII.GetString(InterpreterTestSupport.PageContentBytes(resaved, 0)));
        Assert.Equal(
            InterpreterTestSupport.PageContentBytes(original, 1),
            InterpreterTestSupport.PageContentBytes(resaved, 1));
    }
}
