#nullable enable

using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class SetContentAfterMaterializationTests
{
    private const string Original = "10 10 100 100 re f\n";
    private const string Replacement = "1 1 2 2 re f\n";

    private static PortableDocument LoadedPathDocument()
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes(Original));
        return InterpreterTestSupport.Load(document.ToArray());
    }

    private static string SavedPageContent(PortableDocument document)
        => Encoding.ASCII.GetString(InterpreterTestSupport.PageContentBytes(document.ToArray(), 0));

    [Fact]
    public void SetContent_AfterMaterialization_ContentReflectsNewBytes()
    {
        var loaded = LoadedPathDocument();
        var page = loaded.Pages[0];
        Assert.Single(page.Content);

        page.SetContent(Encoding.ASCII.GetBytes(Replacement));

        Assert.Single(page.Content);
        Assert.DoesNotContain("100", SavedPageContent(loaded));
    }

    [Fact]
    public void SetContent_AfterMaterialization_ThenContentCleared_SavesEmptyContent()
    {
        var loaded = LoadedPathDocument();
        var page = loaded.Pages[0];
        Assert.Single(page.Content);

        page.SetContent(Encoding.ASCII.GetBytes(Replacement));
        page.Content.Clear();

        Assert.DoesNotContain("re", SavedPageContent(loaded));
    }

    [Fact]
    public void SetContent_AfterMaterialization_ThenAppend_KeepsNewBytesAndAddition()
    {
        var loaded = LoadedPathDocument();
        var page = loaded.Pages[0];
        Assert.Single(page.Content);

        page.SetContent(Encoding.ASCII.GetBytes(Replacement));
        var added = page.Content.Add(new PathContent { Stroke = true });
        added.MoveTo(5, 5);
        added.LineTo(7, 7);

        var saved = SavedPageContent(loaded);
        Assert.Contains("1 1 2 2 re", saved);
        Assert.DoesNotContain("10 10 100 100 re", saved);
        Assert.Contains("5 5 m", saved);
    }

    [Fact]
    public void SetContent_AfterMaterialization_ReplacedTextIsGone()
    {
        var document = new PortableDocument();
        document.Pages.Add().Content.Add(new TextContent("Before", 72, 700) { Font = new Font { Size = 12 } });
        var loaded = InterpreterTestSupport.Load(document.ToArray());
        var page = loaded.Pages[0];
        Assert.Single(page.Content);

        page.SetContent(Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (After) Tj ET\n"));

        Assert.DoesNotContain("Before", SavedPageContent(loaded));
        Assert.Contains("After", page.ExtractText());
    }

    [Fact]
    public void SetContent_BeforeMaterialization_StillReusesBytesVerbatim()
    {
        var loaded = LoadedPathDocument();

        loaded.Pages[0].SetContent(Encoding.ASCII.GetBytes(Replacement));

        Assert.Contains("1 1 2 2 re f", SavedPageContent(loaded));
    }
}
