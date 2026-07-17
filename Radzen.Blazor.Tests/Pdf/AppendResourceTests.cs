#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendResourceTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static DocumentBuilder BuildFontAndImage()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Appended Content", BuildTestSupport.Latin);
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        return builder;
    }

    private static Document Reload(Document document)
    {
        using var stream = new MemoryStream(document.ToArray());
        return Document.LoadFromStream(stream);
    }

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
        var target = new Document();
        target.Pages.Add().SetContent(Ascii("BT ET"));
        target.Append(BuildFontAndImage().Build());

        var reader = DocumentReader.Parse(target.ToArray());

        var (hasFont, hasXObject) = ResourceKinds(reader, 1);
        Assert.True(hasFont, "appended built page has /Font resources");
        Assert.True(hasXObject, "appended built page has /XObject resources");
    }

    [Fact]
    public void Append_BuiltSourcePage_ExtractsTextAfterReload()
    {
        var target = new Document();
        target.Pages.Add().SetContent(Ascii("BT ET"));
        target.Append(BuildFontAndImage().Build());

        Assert.Contains("Appended Content", Reload(target).ExtractText());
    }

    [Fact]
    public void Append_LoadedSourcePage_CarriesFontAndImageResources()
    {
        var loaded = Reload(BuildFontAndImage().Build());

        var target = new Document();
        target.Pages.Add().SetContent(Ascii("BT ET"));
        target.Append(loaded);

        var reader = DocumentReader.Parse(target.ToArray());

        var (hasFont, hasXObject) = ResourceKinds(reader, 1);
        Assert.True(hasFont, "appended loaded page has /Font resources");
        Assert.True(hasXObject, "appended loaded page has /XObject resources");
    }

    [Fact]
    public void Append_LoadedSourcePage_ExtractsTextAfterReload()
    {
        var loaded = Reload(BuildFontAndImage().Build());

        var target = new Document();
        target.Pages.Add().SetContent(Ascii("BT ET"));
        target.Append(loaded);

        Assert.Contains("Appended Content", Reload(target).ExtractText());
    }

    [Fact]
    public void Append_LeavesSourceDocumentUsable()
    {
        var loaded = Reload(BuildFontAndImage().Build());

        var target = new Document();
        target.Append(loaded);
        target.ToArray();

        var reader = DocumentReader.Parse(loaded.ToArray());
        var (hasFont, hasXObject) = ResourceKinds(reader, 0);
        Assert.True(hasFont && hasXObject, "source document still carries its resources");
    }
}
