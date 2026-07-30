#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ReverseEncodingTests
{
    [Fact]
    public void Differences_ReversesRemappedCodes()
    {
        var codes = new byte[] { 0x48, 0x69, 0x01, 0x02, 0x03, 0x80 };
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);

        var document = ExtractionSupport.BuildSinglePage(_ => DifferencesFont(), content);

        Assert.Equal("HiéÄ•€", document.Pages[0].ExtractText());
    }

    [Fact]
    public void Differences_WithoutBaseEncoding_UnlistedCodesUseStandardEncoding()
    {
        var codes = new byte[] { 0x41, 0x01, 0x7A };
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);

        var font = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject("Type1"),
            ["BaseFont"] = new NameObject("Helvetica"),
            ["Encoding"] = new DictionaryObject
            {
                ["Type"] = new NameObject("Encoding"),
                ["Differences"] = new ArrayObject { new NumberObject(1), new NameObject("bullet") },
            },
        };

        var document = ExtractionSupport.BuildSinglePage(_ => font, content);

        Assert.Equal("A•z", document.Pages[0].ExtractText());
    }

    [Fact]
    public void Type0Cyrillic_ReversesViaToUnicode()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var sample = "Radzen Привет";

        var document = BuildType0Page(font, sample);

        Assert.Equal(sample, document.Pages[0].ExtractText());
    }

    [Fact]
    public void Type0Cyrillic_MultipleRunsPreserveReadingOrder()
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var top = "Привет";
        var bottom = "Мир";

        var map = Type0EmbedSupport.BuildMap(font, top + bottom);

        var content = new List<byte>();
        content.AddRange(ExtractionSupport.TextRun("F1", 12, 72, 600, Type0EmbedSupport.CompactCodes(font, map, bottom)));
        content.AddRange(ExtractionSupport.TextRun("F1", 12, 72, 700, Type0EmbedSupport.CompactCodes(font, map, top)));
        var document = ExtractionSupport.BuildSinglePage(w => Type0FontEmbedder.Embed(w, Type0FontPlanner.Plan(font, map)), [.. content]);

        var text = document.Pages[0].ExtractText();
        var topIndex = text.IndexOf(top, StringComparison.Ordinal);
        var bottomIndex = text.IndexOf(bottom, StringComparison.Ordinal);

        Assert.True(topIndex >= 0, "top run extracted");
        Assert.True(bottomIndex >= 0, "bottom run extracted");
        Assert.True(topIndex < bottomIndex, "higher baseline extracted first");
    }

    [Fact]
    public void Type0Cjk_ReversesViaToUnicode()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var sample = "中产";

        var document = BuildType0Page(font, sample);

        Assert.Equal(sample, document.Pages[0].ExtractText());
    }

    [Fact]
    public void Type0Mixed_LatinAndCjk()
    {
        var font = Type0EmbedSupport.LoadNoto();
        var sample = "Ab中产";

        var document = BuildType0Page(font, sample);

        Assert.Equal(sample, document.Pages[0].ExtractText());
    }

    private static PortableDocument BuildType0Page(Radzen.Documents.Fonts.Sfnt.SfntFont font, string sample)
    {
        var map = Type0EmbedSupport.BuildMap(font, sample);
        var codes = Type0EmbedSupport.CompactCodes(font, map, sample);
        var content = ExtractionSupport.TextRun("F1", 12, 72, 700, codes);
        return ExtractionSupport.BuildSinglePage(w => Type0FontEmbedder.Embed(w, Type0FontPlanner.Plan(font, map)), content);
    }

    private static DictionaryObject DifferencesFont() => new()
    {
        ["Type"] = new NameObject("Font"),
        ["Subtype"] = new NameObject("Type1"),
        ["BaseFont"] = new NameObject("Helvetica"),
        ["Encoding"] = new DictionaryObject
        {
            ["Type"] = new NameObject("Encoding"),
            ["BaseEncoding"] = new NameObject("WinAnsiEncoding"),
            ["Differences"] = new ArrayObject
            {
                new NumberObject(1), new NameObject("eacute"),
                new NumberObject(2), new NameObject("Adieresis"),
                new NumberObject(3), new NameObject("bullet"),
            },
        },
    };
}
