#nullable enable
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageStampOverlayRegressionTests
{
    private static byte[] Png() => PdfTestResources.ReadAllBytes("Images/rgb.png");

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

    private static string? DoName(string content)
    {
        var match = Regex.Match(content, @"/(\S+)\s+Do\b");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static DictionaryObject XObjects(DocumentReader reader, DictionaryObject page)
    {
        Assert.True(page.TryGetValue("Resources", out var resourcesObject), "page has no /Resources");
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(resourcesObject!));
        Assert.True(resources.TryGetValue("XObject", out var xobjectsObject), "page /Resources has no /XObject");
        return Assert.IsType<DictionaryObject>(reader.Resolve(xobjectsObject!));
    }

    private static string AuthoredContent(string emission)
    {
        var page = Line(emission, "/Type /Page ");
        var reference = Shaped("page", @"/Contents (\d+) 0 R", page);
        return IndirectObject(emission, reference.Groups[1].Value);
    }

    private static string OverlayContent(string emission)
        => IndirectObject(emission, References("page", "Contents", 2, Line(emission, "/Type /Page "))[1]);

    private static void AssertEmittedImageStamp(string emission, string content, string streamName)
    {
        var stamp = Shaped(streamName, @"/(\S+)\s+Do\b", content);
        Carries(streamName, " cm", content);
        Carries(streamName, "q", content);

        var key = stamp.Groups[1].Value;
        var entry = Shaped(
            $"page /Resources /XObject entry for /{key}",
            $@"/XObject <<[^>]*/{Regex.Escape(key)} (\d+) 0 R",
            Line(emission, "/Type /Page "));

        var image = IndirectObject(emission, entry.Groups[1].Value);
        Carries($"image XObject {entry.Groups[1].Value} 0 R", "/Type /XObject", image);
        Carries($"image XObject {entry.Groups[1].Value} 0 R", "/Subtype /Image", image);
        Carries($"image XObject {entry.Groups[1].Value} 0 R", "/Width 64", image);
        Carries($"image XObject {entry.Groups[1].Value} 0 R", "/Height 64", image);
    }

    private static void AssertImageStamp(DocumentReader reader, DictionaryObject page)
    {
        var content = AllContentText(reader, page);
        var key = DoName(content);
        Assert.True(key is not null, "no /Name Do operator emitted for the ImageContent stamp");
        Assert.Contains(" cm", content, StringComparison.Ordinal);
        Assert.Contains("q", content, StringComparison.Ordinal);

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
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent(Png()) { Bounds = PdfRect.FromSize(100, 500, 96, 48) });

        var emission = Emit(document);
        AssertEmittedImageStamp(emission, AuthoredContent(emission), "authored page content");
    }

    [Fact]
    public void ImageStampOnLoadedPage_EmitsVisibleImageXObject()
    {
        var document = FormTestSupport.LoadFixture();
        document.Pages[0].Content.Add(new ImageContent(Png()) { Bounds = PdfRect.FromSize(100, 500, 96, 48) });

        var reader = FormTestSupport.Reload(document);
        AssertImageStamp(reader, FormTestSupport.FirstPage(reader));
    }

    [Fact]
    public void ImageStampOnBuiltPage_RegistersXObjectInResources()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Helvetica");

        var pdf = new DocumentRenderer().Render(document);
        pdf.Pages[0].Content.Add(new ImageContent(Png()) { Bounds = PdfRect.FromSize(72, 72, 96, 48) });

        var emission = Emit(pdf);
        AssertEmittedImageStamp(emission, OverlayContent(emission), "built page overlay");
    }

    [Fact]
    public void ImageStampScalesAndPositionsFromRect()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent(Png()) { Bounds = PdfRect.FromSize(100, 500, 96, 48) });

        var emission = Emit(document);
        var cm = Shaped(
            "authored page content /cm",
            @"([\d.-]+) ([\d.-]+) ([\d.-]+) ([\d.-]+) ([\d.-]+) ([\d.-]+) cm",
            AuthoredContent(emission));

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

        var overlay = reader.DecodeStream(Assert.IsType<StreamObject>(reader.Resolve(contents[1])));
        Assert.Contains(
            ContentStreamTokenizer.Parse(overlay),
            operation => operation.Operator == "Tj"
                && Encoding.Latin1.GetString(operation.Operands[0].Bytes).Contains("STAMP", StringComparison.Ordinal));
    }

    [Fact]
    public void ImageStampOnLoadedPage_KeepsOriginalStreamBytesUntouched()
    {
        var original = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td [(He) -120 (llo)] TJ ET\n");
        var document = ExtractionSupport.BuildSinglePage(_ => HelveticaFont(), original);
        document.Pages[0].Content.Add(new ImageContent(Png()) { Bounds = PdfRect.FromSize(100, 100, 96, 48) });

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
        var codes = Type0EmbedSupport.CompactCodes(sfnt, map, "Hello");
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);

        var document = ExtractionSupport.BuildSinglePage(w => Type0FontEmbedder.Embed(w, Type0FontPlanner.Plan(sfnt, map)), content);
        Assert.Equal("Hello", document.Pages[0].ExtractText());

        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        using var buffer = new MemoryStream(document.ToArray());
        var reloaded = PortableDocument.LoadFromStream(buffer);
        var text = reloaded.Pages[0].ExtractText();

        Assert.Contains("Hello", text, StringComparison.Ordinal);
        Assert.Contains("STAMP", text, StringComparison.Ordinal);
    }
}
