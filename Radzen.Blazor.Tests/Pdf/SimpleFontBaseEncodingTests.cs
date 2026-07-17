#nullable enable
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class SimpleFontBaseEncodingTests
{
    private static readonly byte[] Divergent = [0x41, 0xA5, 0x8E, 0xD0, 0xA0];

    private static DictionaryObject SimpleFont(DocumentObject encoding) => new()
    {
        ["Type"] = new NameObject("Font"),
        ["Subtype"] = new NameObject("Type1"),
        ["BaseFont"] = new NameObject("Helvetica"),
        ["Encoding"] = encoding,
    };

    private static string Extract(DocumentObject encoding, byte[] codes)
    {
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);
        var document = ExtractionSupport.BuildSinglePage(_ => SimpleFont(encoding), content);
        return document.Pages[0].ExtractText();
    }

    [Fact]
    public void NameValuedMacRomanEncoding_DecodesThroughMacRomanTable()
        => Assert.Equal("A•é–†", Extract(new NameObject("MacRomanEncoding"), Divergent));

    [Fact]
    public void BaseEncodingMacRoman_DecodesThroughMacRomanTable()
    {
        var encoding = new DictionaryObject
        {
            ["Type"] = new NameObject("Encoding"),
            ["BaseEncoding"] = new NameObject("MacRomanEncoding"),
        };

        Assert.Equal("A•é–†", Extract(encoding, Divergent));
    }

    [Fact]
    public void BaseEncodingMacRoman_DifferencesOverlayTheBase()
    {
        var encoding = new DictionaryObject
        {
            ["Type"] = new NameObject("Encoding"),
            ["BaseEncoding"] = new NameObject("MacRomanEncoding"),
            ["Differences"] = new ArrayObject { new NumberObject(0xA5), new NameObject("Euro") },
        };

        Assert.Equal("A€é", Extract(encoding, [0x41, 0xA5, 0x8E]));
    }

    [Fact]
    public void NameValuedWinAnsiEncoding_DecodesThroughWinAnsiTable()
        => Assert.Equal("A¥ŽÐ", Extract(new NameObject("WinAnsiEncoding"), [0x41, 0xA5, 0x8E, 0xD0]));

    [Fact]
    public void NoEncoding_StillDecodesThroughWinAnsiTable()
    {
        var font = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject("Type1"),
            ["BaseFont"] = new NameObject("Helvetica"),
        };

        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, [0x41, 0xA5]);
        var document = ExtractionSupport.BuildSinglePage(_ => font, content);

        Assert.Equal("A¥", document.Pages[0].ExtractText());
    }
}
