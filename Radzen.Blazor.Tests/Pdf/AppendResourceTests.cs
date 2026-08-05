#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendResourceTests
{
    private static Document BuildFontAndImage()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Appended Content", BuildTestSupport.Latin);
        section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        return document;
    }

    private static PortableDocument Reload(PortableDocument document)
    {
        using var stream = new MemoryStream(document.ToArray());
        return PortableDocument.LoadFromStream(stream);
    }

    private static string AppendedPage(string emission)
        => IndirectObject(
            emission,
            Shaped("pages node", @"/Kids \[\d+ 0 R (\d+) 0 R", Line(emission, "/Type /Pages ")).Groups[1].Value);

    private static (bool HasFont, bool HasXObject) ResourceKinds(DocumentReader reader, int leafIndex)
    {
        var leaves = BuildTestSupport.PageLeaves(reader);
        var resources = leaves[leafIndex].Resources;
        Assert.NotNull(resources);
        return (
            resources!.TryGetValue("Font", out var f) && reader.Resolve(f!) is DictionaryObject fd && fd.Keys.Count > 0,
            resources.TryGetValue("XObject", out var x) && reader.Resolve(x!) is DictionaryObject xd && xd.Keys.Count > 0);
    }

    [Fact]
    public void Append_BuiltSourcePage_CarriesFontAndImageResources()
    {
        var target = new PortableDocument();
        target.Pages.Add().SetContent(TestBytes.Ascii("BT ET"));
        target.Append(new DocumentRenderer().Render(BuildFontAndImage()));

        var appended = AppendedPage(Emit(target));

        Shaped("appended built page /Font resources", @"/Font << /[^\s/>]+ \d+ 0 R", appended);
        Shaped("appended built page /XObject resources", @"/XObject << /[^\s/>]+ \d+ 0 R", appended);
    }

    [Fact]
    public void Append_BuiltSourcePage_ExtractsTextAfterReload()
    {
        var target = new PortableDocument();
        target.Pages.Add().SetContent(TestBytes.Ascii("BT ET"));
        target.Append(new DocumentRenderer().Render(BuildFontAndImage()));

        Assert.Contains("Appended Content", Reload(target).ExtractText());
    }

    [Fact]
    public void Append_LoadedSourcePage_CarriesFontAndImageResources()
    {
        var loaded = Reload(new DocumentRenderer().Render(BuildFontAndImage()));

        var target = new PortableDocument();
        target.Pages.Add().SetContent(TestBytes.Ascii("BT ET"));
        target.Append(loaded);

        var appended = AppendedPage(Emit(target));

        Shaped("appended loaded page /Font resources", @"/Font << /[^\s/>]+ \d+ 0 R", appended);
        Shaped("appended loaded page /XObject resources", @"/XObject << /[^\s/>]+ \d+ 0 R", appended);
    }

    [Fact]
    public void Append_LoadedSourcePage_ExtractsTextAfterReload()
    {
        var loaded = Reload(new DocumentRenderer().Render(BuildFontAndImage()));

        var target = new PortableDocument();
        target.Pages.Add().SetContent(TestBytes.Ascii("BT ET"));
        target.Append(loaded);

        Assert.Contains("Appended Content", Reload(target).ExtractText());
    }

    [Fact]
    public void Append_LeavesSourceDocumentUsable()
    {
        var loaded = Reload(new DocumentRenderer().Render(BuildFontAndImage()));

        var target = new PortableDocument();
        target.Append(loaded);
        target.ToArray();

        var reader = DocumentReader.Parse(loaded.ToArray());
        var (hasFont, hasXObject) = ResourceKinds(reader, 0);
        Assert.True(hasFont && hasXObject, "source document still carries its resources");
    }
}
