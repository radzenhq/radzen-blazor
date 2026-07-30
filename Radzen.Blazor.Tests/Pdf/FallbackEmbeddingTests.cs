#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FallbackEmbeddingTests
{
    private const string Latin = "Liberation Sans";
    private const string Cjk = "Noto Sans SC";
    private const string Chinese = "中产";
    private const string RunText = "Hi" + Chinese;

    private static SfntFont Noto()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));

    private static int[] NotoCjkGids()
    {
        var noto = Noto();
        var gids = new int[Chinese.Length];
        for (var i = 0; i < Chinese.Length; i++)
        {
            gids[i] = noto.GetGlyphId(Chinese[i]);
            Assert.NotEqual(0, gids[i]);
        }

        return gids;
    }

    private static Document Author()
    {
        var liberation = Type0EmbedSupport.LoadLiberation();
        foreach (var c in Chinese)
        {
            Assert.Equal(0, liberation.GetGlyphId(c));
        }

        var document = new Document();
        document.Fonts.Register(Latin, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        document.Fonts.Register(Cjk, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf")));
        document.Fonts.SetFallback(Latin, Cjk);

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, RunText, Latin);
        return document;
    }

    [Fact]
    public void FallbackFace_IsEmbeddedAsCompactCidKeyedCffSubset()
    {
        var reader = BuildTestSupport.Read(Author());
        var noto = Noto();
        var gids = NotoCjkGids();

        CffFont? fallback = null;
        Dictionary<int, string>? toUnicode = null;
        foreach (var font in BuildTestSupport.Type0Fonts(reader))
        {
            var cff = CidKeyedCff(reader, font);
            if (cff is null)
            {
                continue;
            }

            var cmap = ToUnicode(reader, font);
            if (cmap is not null && cmap.ContainsValue("中") && cmap.ContainsValue("产"))
            {
                fallback = cff;
                toUnicode = cmap;
                break;
            }
        }

        Assert.True(
            fallback is not null,
            "fallback Noto face is not embedded: no CID-keyed FontFile3 recovers the CJK text");

        Assert.Equal(Chinese.Length + 1, fallback!.GlyphCount);
        for (var gid = 0; gid < fallback.GlyphCount; gid++)
        {
            Assert.Equal(gid, fallback.Charset[gid]);
        }

        for (var i = 0; i < Chinese.Length; i++)
        {
            var cid = Type0EmbedSupport.NewGid(toUnicode!, Chinese[i]);
            Assert.InRange(cid, 0, fallback.GlyphCount - 1);
            Assert.Equal(noto.GetAdvanceWidth((ushort)gids[i]), fallback.GetAdvanceWidth(cid));
        }
    }

    [Fact]
    public void FallbackCjk_EmittedUnderFallbackFontResource_NotPrimaryNotdef()
    {
        var reader = BuildTestSupport.Read(Author());

        var leaves = BuildTestSupport.PageLeaves(reader);
        Assert.Single(leaves);
        var (page, resources) = leaves[0];

        var fontResources = FontResources(reader, resources);
        Assert.NotEmpty(fontResources);

        string? fallbackKey = null;
        CffFont? fallback = null;
        Dictionary<int, string>? toUnicode = null;
        var primaryKeys = new List<string>();
        foreach (var (key, fontDict) in fontResources)
        {
            var cff = CidKeyedCff(reader, fontDict);
            var cmap = cff is null ? null : ToUnicode(reader, fontDict);
            if (cmap is not null && cmap.ContainsValue("中") && cmap.ContainsValue("产"))
            {
                fallbackKey = key;
                fallback = cff;
                toUnicode = cmap;
            }
            else
            {
                primaryKeys.Add(key);
            }
        }

        Assert.True(
            fallbackKey is not null,
            "no page /Font resource resolves to the fallback Noto CFF subset");

        var codesByFont = CodesByFont(BuildTestSupport.Content(reader, page));

        Assert.True(codesByFont.TryGetValue(fallbackKey!, out var fallbackCodes),
            "fallback font resource is never selected in the content stream");

        foreach (var ch in Chinese)
        {
            Assert.Contains(Type0EmbedSupport.NewGid(toUnicode!, ch), fallbackCodes!);
        }

        foreach (var code in fallbackCodes!)
        {
            Assert.InRange(code, 0, fallback!.GlyphCount - 1);
        }

        foreach (var key in primaryKeys)
        {
            if (codesByFont.TryGetValue(key, out var codes))
            {
                Assert.DoesNotContain(0, codes!);
            }
        }
    }

    [Fact]
    public void FallbackCjk_RoundTripsThroughExtractText()
    {
        var reloaded = BuildTestSupport.Reload(Author());
        Assert.Contains(Chinese, reloaded.ExtractText(), StringComparison.Ordinal);
    }

    private static CffFont? CidKeyedCff(DocumentReader reader, DictionaryObject fontDict)
    {
        if (!fontDict.TryGetValue("DescendantFonts", out var descendantsObject)
            || reader.Resolve(descendantsObject!) is not ArrayObject descendants
            || descendants.Count == 0
            || reader.Resolve(descendants[0]) is not DictionaryObject descendant
            || !descendant.TryGetValue("FontDescriptor", out var descriptorObject)
            || reader.Resolve(descriptorObject!) is not DictionaryObject descriptor
            || !descriptor.TryGetValue("FontFile3", out var fontFileObject)
            || reader.Resolve(fontFileObject!) is not StreamObject fontFile)
        {
            return null;
        }

        var cff = CffFont.Parse(Type0EmbedSupport.DecodeStream(reader, fontFile));
        return cff.IsCidKeyed ? cff : null;
    }

    private static Dictionary<int, string>? ToUnicode(DocumentReader reader, DictionaryObject fontDict)
    {
        if (!fontDict.TryGetValue("ToUnicode", out var toUnicodeObject)
            || reader.Resolve(toUnicodeObject!) is not StreamObject stream)
        {
            return null;
        }

        return Type0EmbedSupport.ParseToUnicode(Type0EmbedSupport.DecodeStream(reader, stream));
    }

    private static Dictionary<string, DictionaryObject> FontResources(DocumentReader reader, DictionaryObject? resources)
    {
        var map = new Dictionary<string, DictionaryObject>(StringComparer.Ordinal);
        if (resources is null
            || !resources.TryGetValue("Font", out var fontObject)
            || reader.Resolve(fontObject!) is not DictionaryObject fonts)
        {
            return map;
        }

        foreach (var key in fonts.Keys)
        {
            if (reader.Resolve(fonts[key]) is DictionaryObject fontDict)
            {
                map[key] = fontDict;
            }
        }

        return map;
    }

    private static Dictionary<string, List<int>> CodesByFont(byte[] content)
    {
        var result = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var current = "";
        foreach (var op in ContentStreamTokenizer.Parse(content))
        {
            if (op.Operator == "Tf" && op.Operands.Count >= 1 && op.Operands[0].Kind == ContentTokenKind.Name)
            {
                current = op.Operands[0].Text;
            }
            else if (op.Operator == "Tj" && op.Operands.Count >= 1
                && op.Operands[^1].Kind == ContentTokenKind.String)
            {
                var bytes = op.Operands[^1].Bytes;
                if (!result.TryGetValue(current, out var list))
                {
                    list = [];
                    result[current] = list;
                }

                for (var i = 0; i + 1 < bytes.Length; i += 2)
                {
                    list.Add((bytes[i] << 8) | bytes[i + 1]);
                }
            }
        }

        return result;
    }
}
