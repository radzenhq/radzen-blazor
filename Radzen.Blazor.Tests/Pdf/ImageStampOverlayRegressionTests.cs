#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// G4 regression contract.
// (a) ImageContent.EmitBody must emit q / cm / Do positioned from Rect, and
//     SaveToStream must register the image XObject in the page /Resources
//     (mirroring generated-page image registration) so a stamped image is a
//     visible /Subtype /Image XObject on reload.
// (b) Adding content to a LOADED page must append an overlay content stream
//     (/Contents becomes an array) and leave the original stream bytes
//     untouched, instead of re-encoding the whole page through the lossy
//     ContentInterpreter (which garbles Type0 text and drops TJ kerning).
public class ImageStampOverlayRegressionTests
{
    private static byte[] Png() => PdfTestResources.ReadAllBytes("Images/rgb.png");

    // Concatenates the decoded text of every content stream of the page,
    // whether /Contents is a single stream or an array of streams.
    private static string AllContentText(DocumentReader reader, DictionaryObject page)
    {
        Assert.True(page.TryGetValue("Contents", out var contents), "page has no /Contents");
        var resolved = reader.Resolve(contents!);
        if (resolved is StreamObject stream)
        {
            return Encoding.Latin1.GetString(reader.DecodeStream(stream));
        }

        var array = Assert.IsType<ArrayObject>(resolved);
        var sb = new StringBuilder();
        foreach (var entry in array)
        {
            var part = Assert.IsType<StreamObject>(reader.Resolve(entry));
            sb.Append(Encoding.Latin1.GetString(reader.DecodeStream(part)));
            sb.Append('\n');
        }

        return sb.ToString();
    }

    // Finds the operand of the first "/<name> Do" in the content text.
    private static string? DoName(string content)
    {
        var match = System.Text.RegularExpressions.Regex.Match(content, @"/(\S+)\s+Do\b");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static DictionaryObject XObjects(DocumentReader reader, DictionaryObject page)
    {
        Assert.True(page.TryGetValue("Resources", out var resourcesObject), "page has no /Resources");
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(resourcesObject!));
        Assert.True(resources.TryGetValue("XObject", out var xobjectsObject), "page /Resources has no /XObject");
        return Assert.IsType<DictionaryObject>(reader.Resolve(xobjectsObject!));
    }

    private static void AssertImageStamp(DocumentReader reader, DictionaryObject page)
    {
        var content = AllContentText(reader, page);
        var key = DoName(content);
        Assert.True(key is not null, "no /Name Do operator emitted for the ImageContent stamp");
        Assert.Contains(" cm", content, System.StringComparison.Ordinal);
        Assert.Contains("q", content, System.StringComparison.Ordinal);

        var xobjects = XObjects(reader, page);
        Assert.True(xobjects.TryGetValue(key!, out var imageObject),
            $"content references /{key} Do but /Resources /XObject has no entry '{key}'");
        var image = Assert.IsType<StreamObject>(reader.Resolve(imageObject!));
        Assert.Equal("Image", ((NameObject)image.Dictionary["Subtype"]).Value);
        Assert.Equal("XObject", ((NameObject)image.Dictionary["Type"]).Value);
        Assert.Equal(64, ((NumberObject)image.Dictionary["Width"]).IntValue);
        Assert.Equal(64, ((NumberObject)image.Dictionary["Height"]).IntValue);
    }

    [Fact]
    public void ImageStampOnAuthoredPage_EmitsVisibleImageXObject()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent(Png()) { Rect = new Rect(100, 500, 96, 48) });

        var reader = DocumentReader.Parse(document.ToArray());
        AssertImageStamp(reader, FormTestSupport.FirstPage(reader));
    }

    [Fact]
    public void ImageStampOnLoadedPage_EmitsVisibleImageXObject()
    {
        var document = FormTestSupport.LoadFixture();
        document.Pages[0].Content.Add(new ImageContent(Png()) { Rect = new Rect(100, 500, 96, 48) });

        var reader = FormTestSupport.Reload(document);
        AssertImageStamp(reader, FormTestSupport.FirstPage(reader));
    }

    [Fact]
    public void ImageStampOnBuiltPage_RegistersXObjectInResources()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Helvetica");

        var document = builder.Build();
        document.Pages[0].Content.Add(new ImageContent(Png()) { Rect = new Rect(72, 72, 96, 48) });

        var reader = DocumentReader.Parse(document.ToArray());
        AssertImageStamp(reader, FormTestSupport.FirstPage(reader));
    }

    [Fact]
    public void ImageStampScalesAndPositionsFromRect()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent(Png()) { Rect = new Rect(100, 500, 96, 48) });

        var reader = DocumentReader.Parse(document.ToArray());
        var content = AllContentText(reader, FormTestSupport.FirstPage(reader));

        // Image space is the unit square, so the cm matrix must carry the Rect
        // width/height as scale and the Rect origin as translation.
        var cm = System.Text.RegularExpressions.Regex.Match(
            content, @"([\d.-]+) ([\d.-]+) ([\d.-]+) ([\d.-]+) ([\d.-]+) ([\d.-]+) cm");
        Assert.True(cm.Success, "no cm operator emitted for the ImageContent stamp");
        Assert.Equal("96", cm.Groups[1].Value);
        Assert.Equal("48", cm.Groups[4].Value);
        Assert.Equal("100", cm.Groups[5].Value);
    }

    private static DictionaryObject HelveticaFont() => new()
    {
        ["Type"] = new NameObject("Font"),
        ["Subtype"] = new NameObject("Type1"),
        ["BaseFont"] = new NameObject("Helvetica"),
        ["Encoding"] = new NameObject("WinAnsiEncoding"),
    };

    [Fact]
    public void TextStampOnLoadedPage_AppendsOverlayStreamKeepingOriginalBytes()
    {
        var original = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td [(He) -120 (llo)] TJ ET\n");
        var document = ExtractionSupport.BuildSinglePage(_ => HelveticaFont(), original);
        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        var reader = DocumentReader.Parse(document.ToArray());
        var page = FormTestSupport.FirstPage(reader);

        Assert.True(page.TryGetValue("Contents", out var contentsObject));
        var contents = Assert.IsType<ArrayObject>(reader.Resolve(contentsObject!));
        Assert.Equal(2, contents.Count);

        var first = Assert.IsType<StreamObject>(reader.Resolve(contents[0]));
        Assert.Equal(original, reader.DecodeStream(first));

        var overlay = Encoding.Latin1.GetString(
            reader.DecodeStream(Assert.IsType<StreamObject>(reader.Resolve(contents[1]))));
        Assert.Contains("STAMP", overlay, System.StringComparison.Ordinal);
        Assert.Contains("Tj", overlay, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ImageStampOnLoadedPage_KeepsOriginalStreamBytesUntouched()
    {
        var original = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td [(He) -120 (llo)] TJ ET\n");
        var document = ExtractionSupport.BuildSinglePage(_ => HelveticaFont(), original);
        document.Pages[0].Content.Add(new ImageContent(Png()) { Rect = new Rect(100, 100, 96, 48) });

        var reader = DocumentReader.Parse(document.ToArray());
        var page = FormTestSupport.FirstPage(reader);

        Assert.True(page.TryGetValue("Contents", out var contentsObject));
        var contents = Assert.IsType<ArrayObject>(reader.Resolve(contentsObject!));
        Assert.Equal(2, contents.Count);
        Assert.Equal(original, reader.DecodeStream(Assert.IsType<StreamObject>(reader.Resolve(contents[0]))));

        AssertImageStamp(reader, page);
    }

    [Fact]
    public void TextStampOnLoadedType0Page_OriginalTextStillExtracts()
    {
        var sfnt = Type0EmbedSupport.LoadLiberation();
        var map = Type0EmbedSupport.BuildMap(sfnt, "Hello");
        var codes = ExtractionSupport.IdentityCodes(sfnt, "Hello");
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);

        var document = ExtractionSupport.BuildSinglePage(w => Type0FontEmbedder.Embed(w, sfnt, map), content);
        Assert.Equal("Hello", document.Pages[0].ExtractText());

        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        using var buffer = new MemoryStream(document.ToArray());
        var reloaded = Document.LoadFromStream(buffer);
        var text = reloaded.Pages[0].ExtractText();

        Assert.Contains("Hello", text, System.StringComparison.Ordinal);
        Assert.Contains("STAMP", text, System.StringComparison.Ordinal);
    }
}
