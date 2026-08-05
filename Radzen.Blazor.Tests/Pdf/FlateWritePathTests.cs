#nullable enable
using System;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class FlateWritePathTests
{
    private static Document LatinBuilder(string text)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Latin);
        return document;
    }

    private static byte[] RequireFlate(DocumentReader reader, StreamObject stream)
    {
        Assert.True(
            stream.Dictionary.TryGetValue("Filter", out var filter) && filter is not null,
            "stream must declare /Filter /FlateDecode");
        var name = Assert.IsType<NameObject>(reader.Resolve(filter!));
        Assert.Equal("FlateDecode", name.Value);
        return FlateFilter.Decode(stream.Data.ToArray());
    }

    private static StreamObject FirstPageContents(DocumentReader reader)
    {
        var (page, _) = Assert.Single(BuildTestSupport.PageLeaves(reader));
        return (StreamObject)reader.Resolve(page["Contents"]);
    }

    private static DictionaryObject SingleType0Descriptor(DocumentReader reader, out DictionaryObject top)
    {
        top = Assert.Single(BuildTestSupport.Type0Fonts(reader));
        var descendants = (ArrayObject)reader.Resolve(top["DescendantFonts"]);
        var descendant = (DictionaryObject)reader.Resolve(descendants[0]);
        return (DictionaryObject)reader.Resolve(descendant["FontDescriptor"]);
    }

    [Fact]
    public void GeneratedContentStream_IsFlateEncoded_AndDecodesToOperators()
    {
        var reader = BuildTestSupport.Read(LatinBuilder("Compressed content stream"));

        var contents = FirstPageContents(reader);
        var decoded = RequireFlate(reader, contents);

        var operators = ContentOperationTestHelpers.Operators(decoded);
        Assert.Contains("BT", operators);
        Assert.Contains("Tf", operators);
    }

    [Fact]
    public void FontFile2_IsFlateEncoded_WithLength1OfUncompressedSubset()
    {
        var reader = BuildTestSupport.Read(LatinBuilder("Subset glyphs"));

        var descriptor = SingleType0Descriptor(reader, out _);
        var fontFile = (StreamObject)reader.Resolve(descriptor["FontFile2"]);
        var decoded = RequireFlate(reader, fontFile);

        Assert.True(decoded.Length > 12, "subset has sfnt header");
        Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x00 }, decoded[..4]);

        var length1 = (NumberObject)reader.Resolve(fontFile.Dictionary["Length1"]);
        Assert.Equal(decoded.Length, length1.IntValue);
    }

    [Fact]
    public void FontFile3_IsFlateEncoded_AndKeepsCidFontType0CSubtype()
    {
        var document = new Document();
        BuildTestSupport.RegisterCjk(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "你好", BuildTestSupport.Cjk);
        var reader = BuildTestSupport.Read(document);

        var descriptor = SingleType0Descriptor(reader, out _);
        var fontFile = (StreamObject)reader.Resolve(descriptor["FontFile3"]);
        var decoded = RequireFlate(reader, fontFile);

        Assert.True(decoded.Length > 4, "subset has CFF header");
        Assert.Equal(1, decoded[0]);
        Assert.Equal(0, decoded[1]);

        var subtype = (NameObject)reader.Resolve(fontFile.Dictionary["Subtype"]);
        Assert.Equal("CIDFontType0C", subtype.Value);
    }

    [Fact]
    public void ToUnicodeCMap_IsFlateEncoded()
    {
        var reader = BuildTestSupport.Read(LatinBuilder("Unicode map"));

        SingleType0Descriptor(reader, out var top);
        var toUnicode = (StreamObject)reader.Resolve(top["ToUnicode"]);
        var decoded = RequireFlate(reader, toUnicode);

        var text = Encoding.Latin1.GetString(decoded);
        Assert.Contains("begincmap", text, StringComparison.Ordinal);
        Assert.Contains("beginbfchar", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CidSet_IsFlateEncoded()
    {
        var reader = BuildTestSupport.Read(LatinBuilder("CID set"));

        var descriptor = SingleType0Descriptor(reader, out _);
        var cidSet = (StreamObject)reader.Resolve(descriptor["CIDSet"]);
        var decoded = RequireFlate(reader, cidSet);

        Assert.True(decoded.Length > 0, "CIDSet bitmap is non-empty");
        Assert.Contains(decoded, b => b != 0);
    }

    [Fact]
    public void CompressedOutput_StillRoundTripsThroughLoadAndExtractText()
    {
        var document = LatinBuilder("Flate round trip payload");

        var text = BuildTestSupport.Reload(document).ExtractText();

        Assert.Contains("Flate round trip payload", text, StringComparison.Ordinal);
    }

    [Fact]
    public void JpegXObject_KeepsDctDecodeOnly_NoDoubleCompression()
    {
        var original = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));

        var emission = Encoding.Latin1.GetString(new DocumentRenderer().ToArray(document));
        var images = Regex.Matches(emission, ImageObjectPattern);
        Assert.True(images.Count == 1, $"Expected exactly 1 image XObject in the emission, found {images.Count}.");

        var body = IndirectObject(emission, images[0].Groups[1].Value);
        Carries("image XObject", "/Filter /DCTDecode", body);

        var payload = StreamPayload("image XObject", body);
        Assert.True(
            payload == Encoding.Latin1.GetString(original),
            "The image XObject payload is not the original JPEG bytes;"
            + $" expected {original.Length} bytes, emitted {payload.Length}.");
    }

    private const string ImageObjectPattern = @"\n(\d+) 0 obj\n<< [^\n]*/Subtype /Image[^\n]*>>\nstream\n";

    private static string StreamPayload(string subject, string body)
    {
        const string opening = "\nstream\n";
        const string closing = "\nendstream";

        var start = body.IndexOf(opening, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{subject} is not a stream object.\n{subject}:\n{Excerpt(body)}");

        var end = body.LastIndexOf(closing, StringComparison.Ordinal);
        Assert.True(end > start, $"{subject} has no endstream.\n{subject}:\n{Excerpt(body)}");

        return body[(start + opening.Length)..end];
    }

    [Fact]
    public void PngXObject_KeepsSingleFlateFilter_NoDoubleCompression()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.png")));
        var reader = BuildTestSupport.Read(document);

        var image = Assert.Single(BuildTestSupport.ImageXObjects(reader));
        var filter = Assert.IsType<NameObject>(reader.Resolve(image.Dictionary["Filter"]));
        Assert.Equal("FlateDecode", filter.Value);

        var width = BuildTestSupport.Int(image.Dictionary, "Width");
        var height = BuildTestSupport.Int(image.Dictionary, "Height");
        var samples = FlateFilter.Decode(image.Data.ToArray());
        Assert.Equal(width * height * 3, samples.Length);
    }
}
