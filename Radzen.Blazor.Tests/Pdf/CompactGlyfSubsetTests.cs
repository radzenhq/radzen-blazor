#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Package C1: compact TrueType (glyf) subsetting through the full Build() pipeline.
// The embedded CIDFontType2 subset renumbers glyphs into a contiguous 0..N-1 space
// (used glyphs + composite closure + .notdef), the content stream emits the NEW
// compact gid as the 2-byte Identity-H code (so CID == new gid, CIDToGIDMap stays
// Identity), and W / CIDSet / ToUnicode are keyed by the new gid. The CIDSet must
// mark exactly 0..N-1 - every glyph in the font program - per veraPDF 6.2.11.4.2.
//
// Fixture: LiberationSans-Regular.ttf (2620 glyphs). The sample mixes English,
// Cyrillic and precomposed accents so the closure pulls in composite components.
public class CompactGlyfSubsetTests
{
    private const string Sample = "Naïve café fjord - Мой рай!";

    private static DocumentReader BuildReader()
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, Sample, BuildTestSupport.Latin);
        return BuildTestSupport.Read(builder);
    }

    private static DocumentBuilder Builder()
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, Sample, BuildTestSupport.Latin);
        return builder;
    }

    private static (DictionaryObject Top, DictionaryObject Descendant, DictionaryObject Descriptor) FontGraph(DocumentReader reader)
    {
        var top = Assert.Single(BuildTestSupport.Type0Fonts(reader));
        var descendants = Assert.IsType<ArrayObject>(reader.Resolve(top["DescendantFonts"]));
        var descendant = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(descendants)));
        var descriptor = Assert.IsType<DictionaryObject>(reader.Resolve(descendant["FontDescriptor"]));
        return (top, descendant, descriptor);
    }

    private static byte[] FontFile2(DocumentReader reader, DictionaryObject descriptor)
    {
        var stream = Assert.IsType<StreamObject>(reader.Resolve(descriptor["FontFile2"]));
        return reader.DecodeStream(stream);
    }

    private static HashSet<int> ExpectedClosure(SfntFont original)
    {
        var used = new HashSet<int>();
        foreach (var ch in Sample)
        {
            var gid = original.GetGlyphId(ch);
            Assert.NotEqual(0, gid);
            used.Add(gid);
        }

        var closure = Type0EmbedSupport.GlyfClosure(original, used);
        Assert.True(closure.Count > used.Count + 1, "sample must pull composite component glyphs");
        return closure;
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
    public void FontFile2_HasCompactGlyphSpace()
    {
        var reader = BuildReader();
        var (_, _, descriptor) = FontGraph(reader);
        var bytes = FontFile2(reader, descriptor);
        var subset = SfntFont.Parse(bytes);

        var original = Type0EmbedSupport.LoadLiberation();
        var expected = ExpectedClosure(original);

        Assert.Equal(expected.Count, (int)subset.GlyphCount);
        Assert.True(subset.GlyphCount < original.GlyphCount / 10,
            $"numGlyphs {subset.GlyphCount} must be compact, not the original {original.GlyphCount}");
    }

    [Fact]
    public void FontFile2_LocaAndHmtxAreCompact()
    {
        var reader = BuildReader();
        var (_, _, descriptor) = FontGraph(reader);
        var bytes = FontFile2(reader, descriptor);
        var subset = SfntFont.Parse(bytes);
        var n = (int)subset.GlyphCount;

        var loca = SfntChecksumValidator.ReadLocaOffsets(bytes);
        Assert.Equal(n + 1, loca.Length);
        for (var i = 1; i < loca.Length; i++)
        {
            Assert.True(loca[i] >= loca[i - 1], $"loca not monotonic at {i}");
        }

        Assert.True(subset.TryGetTable("glyf", out var glyf));
        Assert.Equal((uint)glyf.Length, loca[^1]);

        Assert.True(subset.TryGetTable("hmtx", out var hmtx));
        Assert.True(hmtx.Length <= 4 * n, $"hmtx {hmtx.Length} bytes exceeds compact bound {4 * n}");
    }

    [Fact]
    public void FontFile2_IsSmall()
    {
        var reader = BuildReader();
        var (_, _, descriptor) = FontGraph(reader);
        var bytes = FontFile2(reader, descriptor);

        // Identity subsetting carried full-length loca/hmtx/cmap/name (tens of KB);
        // the compact subset for ~30 glyphs must stay in the single-digit KB range.
        Assert.True(bytes.Length < 16_000, $"embedded FontFile2 is {bytes.Length} bytes; expected a compact subset");
    }

    [Fact]
    public void CidSet_MarksExactlyAllCompactGids()
    {
        var reader = BuildReader();
        var (_, _, descriptor) = FontGraph(reader);
        var n = (int)SfntFont.Parse(FontFile2(reader, descriptor)).GlyphCount;

        Assert.True(descriptor.ContainsKey("CIDSet"), "FontDescriptor must keep /CIDSet");
        var stream = Assert.IsType<StreamObject>(reader.Resolve(descriptor["CIDSet"]));
        var bits = Type0EmbedSupport.SetBits(reader.DecodeStream(stream));

        Assert.Equal(Enumerable.Range(0, n).ToHashSet(), bits);
    }

    [Fact]
    public void Descendant_KeepsIdentityMappings()
    {
        var reader = BuildReader();
        var (top, descendant, _) = FontGraph(reader);

        Assert.Equal("Identity-H", BuildTestSupport.Name(reader, top, "Encoding"));
        Assert.Equal("Identity", BuildTestSupport.Name(reader, descendant, "CIDToGIDMap"));
    }

    [Fact]
    public void ContentStream_EmitsCompactGidCodes()
    {
        var reader = BuildReader();
        var (top, _, descriptor) = FontGraph(reader);
        var n = (int)SfntFont.Parse(FontFile2(reader, descriptor)).GlyphCount;
        var toUnicode = ToUnicode(reader, top);

        var codes = ShownCodes(reader);
        Assert.NotEmpty(codes);

        var shown = new StringBuilder();
        foreach (var code in codes)
        {
            Assert.True(code < n, $"content-stream code {code} must be a compact gid < numGlyphs {n}");
            Assert.True(toUnicode.ContainsKey(code), $"ToUnicode must map compact code {code}");
            shown.Append(toUnicode[code]);
        }

        // Every non-space character of the sample is shown under its compact code.
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
    public void WWidths_AreKeyedByCompactGids()
    {
        var reader = BuildReader();
        var (top, descendant, descriptor) = FontGraph(reader);
        var n = (int)SfntFont.Parse(FontFile2(reader, descriptor)).GlyphCount;
        var toUnicode = ToUnicode(reader, top);
        var original = Type0EmbedSupport.LoadLiberation();

        var widths = Type0EmbedSupport.ParseWidths(reader, (ArrayObject)reader.Resolve(descendant["W"]));
        foreach (var cid in widths.Keys)
        {
            Assert.True(cid < n, $"W entry for CID {cid} is outside the compact glyph space 0..{n - 1}");
        }

        foreach (var ch in Sample.Distinct())
        {
            if (ch == ' ')
            {
                continue;
            }

            var newGid = Type0EmbedSupport.NewGid(toUnicode, ch);
            Assert.True(widths.ContainsKey(newGid), $"W missing compact CID {newGid} for '{ch}'");
            Assert.Equal(Type0EmbedSupport.ScaleWidth(original, original.GetGlyphId(ch)), widths[newGid]);
        }
    }

    [Fact]
    public void ExtractText_RoundTripsEnglishAndCyrillic()
    {
        var reloaded = BuildTestSupport.Reload(Builder());
        var text = reloaded.ExtractText();

        Assert.Contains("Naïve café fjord", text, StringComparison.Ordinal);
        Assert.Contains("Мой рай", text, StringComparison.Ordinal);
    }
}
