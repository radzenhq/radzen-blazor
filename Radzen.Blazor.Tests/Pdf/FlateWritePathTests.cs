#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

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

        var text = Encoding.Latin1.GetString(decoded);
        Assert.Contains("BT", text, StringComparison.Ordinal);
        Assert.Contains("Tf", text, StringComparison.Ordinal);
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
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        var reader = BuildTestSupport.Read(document);

        var image = Assert.Single(BuildTestSupport.ImageXObjects(reader));
        var filter = Assert.IsType<NameObject>(reader.Resolve(image.Dictionary["Filter"]));
        Assert.Equal("DCTDecode", filter.Value);
        Assert.Equal(original, image.Data);
    }

    [Fact]
    public void PngXObject_KeepsSingleFlateFilter_NoDoubleCompression()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.png"));
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
