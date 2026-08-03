#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CompactCffSubsetTests
{
    private const string Sample = "Ab Мир 中产";

    private static Document Builder()
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterCjk(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, Sample, BuildTestSupport.Cjk);
        return document;
    }

    private static DocumentReader BuildReader() => BuildTestSupport.Read(Builder());

    private static (DictionaryObject Top, DictionaryObject Descendant, DictionaryObject Descriptor) FontGraph(DocumentReader reader)
    {
        var top = Assert.Single(BuildTestSupport.Type0Fonts(reader));
        var descendants = Assert.IsType<ArrayObject>(reader.Resolve(top["DescendantFonts"]));
        var descendant = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(descendants)));
        var descriptor = Assert.IsType<DictionaryObject>(reader.Resolve(descendant["FontDescriptor"]));
        return (top, descendant, descriptor);
    }

    private static CffFont FontFile3(DocumentReader reader, DictionaryObject descriptor)
    {
        var stream = Assert.IsType<StreamObject>(reader.Resolve(descriptor["FontFile3"]));
        return CffFont.Parse(reader.DecodeStream(stream));
    }

    private static HashSet<int> UsedOriginalGids()
    {
        var original = Type0EmbedSupport.LoadNoto();
        var used = new HashSet<int>();
        foreach (var ch in Sample)
        {
            var gid = original.GetGlyphId(ch);
            Assert.NotEqual(0, gid);
            used.Add(gid);
        }

        return used;
    }

    private static Dictionary<int, string> ToUnicode(DocumentReader reader, DictionaryObject top)
    {
        var stream = Assert.IsType<StreamObject>(reader.Resolve(top["ToUnicode"]));
        return Type0EmbedSupport.ParseToUnicode(reader.DecodeStream(stream));
    }

    private static List<int> ShownCodes(DocumentReader reader)
    {
        var codes = new List<int>();
        foreach (var (page, _) in BuildTestSupport.PageLeaves(reader))
        {
            foreach (var op in ContentStreamTokenizer.Parse(BuildTestSupport.Content(reader, page)))
            {
                if (op.Operator is not ("Tj" or "TJ"))
                {
                    continue;
                }

                foreach (var operand in op.Operands)
                {
                    if (operand.Kind != ContentTokenKind.String)
                    {
                        continue;
                    }

                    var bytes = operand.Bytes;
                    for (var i = 0; i + 1 < bytes.Length; i += 2)
                    {
                        codes.Add((bytes[i] << 8) | bytes[i + 1]);
                    }
                }
            }
        }

        return codes;
    }

    [Fact]
    public void FontFile3_HasCompactIdentityCidSpace()
    {
        var reader = BuildReader();
        var (_, _, descriptor) = FontGraph(reader);
        var cff = FontFile3(reader, descriptor);

        Assert.True(cff.IsCidKeyed);
        Assert.Equal(UsedOriginalGids().Count + 1, cff.GlyphCount);

        for (var gid = 0; gid < cff.GlyphCount; gid++)
        {
            Assert.Equal(gid, cff.Charset[gid]);
        }
    }

    [Fact]
    public void ContentStream_EmitsCompactCidCodes()
    {
        var reader = BuildReader();
        var (top, _, descriptor) = FontGraph(reader);
        var n = FontFile3(reader, descriptor).GlyphCount;
        var toUnicode = ToUnicode(reader, top);

        var codes = ShownCodes(reader);
        Assert.NotEmpty(codes);

        var shown = new StringBuilder();
        foreach (var code in codes)
        {
            Assert.True(code < n, $"content-stream code {code} must be a compact id < glyph count {n}");
            Assert.True(toUnicode.ContainsKey(code), $"ToUnicode must map compact code {code}");
            shown.Append(toUnicode[code]);
        }

        var text = shown.ToString();
        foreach (var ch in Sample.Distinct())
        {
            if (ch != ' ')
            {
                Assert.Contains(ch, text);
            }
        }
    }

    [Fact]
    public void WWidths_AreKeyedByCompactCids()
    {
        var reader = BuildReader();
        var (top, descendant, descriptor) = FontGraph(reader);
        var n = FontFile3(reader, descriptor).GlyphCount;
        var toUnicode = ToUnicode(reader, top);
        var original = Type0EmbedSupport.LoadNoto();

        var widths = Type0EmbedSupport.ParseWidths(reader, (ArrayObject)reader.Resolve(descendant["W"]));
        foreach (var cid in widths.Keys)
        {
            Assert.True(cid < n, $"W entry for CID {cid} is outside the compact space 0..{n - 1}");
        }

        foreach (var ch in Sample.Distinct())
        {
            if (ch == ' ')
            {
                continue;
            }

            var newCid = Type0EmbedSupport.NewGid(toUnicode, ch);
            Assert.True(widths.ContainsKey(newCid), $"W missing compact CID {newCid} for '{ch}'");
            Assert.Equal(Type0EmbedSupport.NotoSansScWidth(ch), widths[newCid]);
        }
    }

    [Fact]
    public void FontFile3_PreservesAdvanceWidthsAtCompactIds()
    {
        var reader = BuildReader();
        var (top, _, descriptor) = FontGraph(reader);
        var subset = FontFile3(reader, descriptor);
        var toUnicode = ToUnicode(reader, top);

        var sfnt = Type0EmbedSupport.LoadNoto();
        Assert.True(sfnt.TryGetTable("CFF ", out var cffData));
        var original = CffFont.Parse(cffData);

        foreach (var ch in Sample.Distinct())
        {
            if (ch == ' ')
            {
                continue;
            }

            var newCid = Type0EmbedSupport.NewGid(toUnicode, ch);
            Assert.Equal(original.GetAdvanceWidth(sfnt.GetGlyphId(ch)), subset.GetAdvanceWidth(newCid));
        }
    }

    [Fact]
    public void ExtractText_RoundTripsCjk()
    {
        var reloaded = BuildTestSupport.Reload(Builder());
        var text = reloaded.ExtractText();

        Assert.Contains("Ab", text, StringComparison.Ordinal);
        Assert.Contains("Мир", text, StringComparison.Ordinal);
        Assert.Contains("中产", text, StringComparison.Ordinal);
    }
}
