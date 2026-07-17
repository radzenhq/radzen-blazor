#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// veraPDF clause 6.2.11.4.2: /CIDSet must identify all CIDs in the embedded subset.
public class PdfACidSetExactTests
{
    private const string LatinSample = "Voilà - le café naïve! Мой рай";
    private const string CjkSample = "Ab Мир 中产";

    private static DocumentReader Build(Action<DocumentBuilder> register, string family, string text)
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3B };
        register(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, text, family);
        return BuildTestSupport.Read(builder);
    }

    private static DictionaryObject Descriptor(DocumentReader reader)
    {
        var top = Assert.Single(BuildTestSupport.Type0Fonts(reader));
        var descendants = Assert.IsType<ArrayObject>(reader.Resolve(top["DescendantFonts"]));
        Assert.Equal(1, descendants.Count);
        var descendant = Assert.IsType<DictionaryObject>(reader.Resolve(descendants[0]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(descendant["FontDescriptor"]));
    }

    private static HashSet<int> CidSetBits(DocumentReader reader, DictionaryObject descriptor)
    {
        Assert.True(descriptor.ContainsKey("CIDSet"), "FontDescriptor must keep /CIDSet");
        var stream = Assert.IsType<StreamObject>(reader.Resolve(descriptor["CIDSet"]));
        return Type0EmbedSupport.SetBits(reader.DecodeStream(stream));
    }

    private static HashSet<int> UsedGids(SfntFont font, string text)
    {
        var set = new HashSet<int>();
        foreach (var ch in text)
        {
            var gid = font.GetGlyphId(ch);
            if (gid != 0)
            {
                set.Add(gid);
            }
        }

        return set;
    }

    private static void AssertSameCids(HashSet<int> embedded, HashSet<int> bits)
    {
        var missing = embedded.Except(bits).Order().ToArray();
        var extra = bits.Except(embedded).Order().ToArray();
        Assert.True(missing.Length == 0,
            $"CIDSet is missing embedded glyphs: {string.Join(", ", missing)}");
        Assert.True(extra.Length == 0,
            $"CIDSet marks CIDs not present in the embedded subset: {string.Join(", ", extra)}");
    }

    [Fact]
    public void PdfA3B_LiberationGlyfSubset_IsCompactAndCidSetMarksAllGids()
    {
        var reader = Build(BuildTestSupport.RegisterLatin, BuildTestSupport.Latin, LatinSample);
        var descriptor = Descriptor(reader);

        var fontFile = Assert.IsType<StreamObject>(reader.Resolve(descriptor["FontFile2"]));
        var bytes = reader.DecodeStream(fontFile);
        var subset = SfntFont.Parse(bytes);

        var original = Type0EmbedSupport.LoadLiberation();
        var used = UsedGids(original, LatinSample);
        var expected = Type0EmbedSupport.GlyfClosure(original, used);

        Assert.True(expected.Count > used.Count + 1, "sample must embed composite component glyphs");

        var n = (int)subset.GlyphCount;
        Assert.Equal(expected.Count, n);
        Assert.True(n < original.GlyphCount / 10,
            $"numGlyphs {n} must be compact, not the original {original.GlyphCount}");

        var loca = SfntChecksumValidator.ReadLocaOffsets(bytes);
        Assert.Equal(n + 1, loca.Length);
        Assert.True(subset.TryGetTable("glyf", out var glyf));
        Assert.Equal((uint)glyf.Length, loca[^1]);

        AssertSameCids(Enumerable.Range(0, n).ToHashSet(), CidSetBits(reader, descriptor));
    }

    [Fact]
    public void PdfA3B_NotoCffSubset_IsCompactAndCidSetMarksAllCids()
    {
        var reader = Build(BuildTestSupport.RegisterCjk, BuildTestSupport.Cjk, CjkSample);
        var descriptor = Descriptor(reader);

        var fontFile = Assert.IsType<StreamObject>(reader.Resolve(descriptor["FontFile3"]));
        var cff = CffFont.Parse(reader.DecodeStream(fontFile));

        var used = UsedGids(Type0EmbedSupport.LoadNoto(), CjkSample);
        var n = cff.GlyphCount;
        Assert.Equal(used.Count + 1, n);
        for (var gid = 0; gid < n; gid++)
        {
            Assert.Equal(gid, cff.Charset[gid]);
        }

        AssertSameCids(Enumerable.Range(0, n).ToHashSet(), CidSetBits(reader, descriptor));
    }
}
