#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class Type0CompactGidSingleSourceTests
{
    [Fact]
    public void EmbeddedSubset_PlacesGlyphsAtTheSuppliedCompactIndices()
    {
        var font = Type0EmbedSupport.LoadLiberation();

        var gidToUnicode = new Dictionary<ushort, int>();
        foreach (var ch in "Helo")
        {
            gidToUnicode[font.GetGlyphId(ch)] = ch;
        }

        var closure = Type0EmbedSupport.GlyfClosure(font, gidToUnicode.Keys.Select(gid => (int)gid))
            .Select(gid => (ushort)gid)
            .OrderBy(gid => gid)
            .ToList();

        var reversed = Enumerable.Reverse(closure).ToList();
        var compactMap = new Dictionary<ushort, ushort>();
        for (var i = 0; i < reversed.Count; i++)
        {
            compactMap[reversed[i]] = (ushort)i;
        }

        Assert.NotEqual(GlyfSubsetter.BuildCompactGidMap(font, gidToUnicode.Keys), compactMap);

        var subset = SfntFont.Parse(EmbedFontFile2(font, gidToUnicode, compactMap));
        var outlines = SubsetOutlines(subset);

        foreach (var gid in closure)
        {
            var expected = StripSimpleInstructions(Type0EmbedSupport.OutlineBytes(font, gid));
            Assert.True(outlines[compactMap[gid]].AsSpan().SequenceEqual(expected),
                $"glyph {gid} not placed at supplied compact index {compactMap[gid]}");
        }
    }

    private static byte[] EmbedFontFile2(SfntFont font, IReadOnlyDictionary<ushort, int> gidToUnicode, IReadOnlyDictionary<ushort, ushort> compactMap)
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer);
        var topRef = Type0FontEmbedder.Embed(writer, Type0FontPlanner.Plan(font, gidToUnicode, compactMap));
        writer.Close();

        var reader = DocumentReader.Parse(buffer.ToArray());
        var top = (DictionaryObject)reader.Resolve(reader.GetObject(topRef.ObjectNumber));
        var descendant = (DictionaryObject)reader.Resolve(((ArrayObject)reader.Resolve(top["DescendantFonts"]))[0]);
        var descriptor = (DictionaryObject)reader.Resolve(descendant["FontDescriptor"]);
        var fontFile = (StreamObject)reader.Resolve(descriptor["FontFile2"]);
        return Type0EmbedSupport.DecodeStream(reader, fontFile);
    }

    private static byte[][] SubsetOutlines(SfntFont subset)
    {
        Assert.True(subset.TryGetTable("glyf", out var glyf));
        Assert.True(subset.TryGetTable("loca", out var loca));
        Assert.True(subset.TryGetTable("head", out var head));
        var longLoca = ((head[50] << 8) | head[51]) != 0;

        uint Offset(int index) => longLoca
            ? (uint)((loca[index * 4] << 24) | (loca[index * 4 + 1] << 16) | (loca[index * 4 + 2] << 8) | loca[index * 4 + 3])
            : (uint)(((loca[index * 2] << 8) | loca[index * 2 + 1]) * 2);

        var outlines = new byte[subset.GlyphCount][];
        for (var gid = 0; gid < subset.GlyphCount; gid++)
        {
            outlines[gid] = glyf[(int)Offset(gid)..(int)Offset(gid + 1)];
        }

        return outlines;
    }

    private static byte[] StripSimpleInstructions(byte[] g)
    {
        if (g.Length < 10 || (short)((g[0] << 8) | g[1]) < 0)
        {
            return g;
        }

        var contours = (short)((g[0] << 8) | g[1]);
        var instrLenPos = 10 + contours * 2;
        var instrLen = (g[instrLenPos] << 8) | g[instrLenPos + 1];
        var headLen = instrLenPos + 2;
        var tailStart = headLen + instrLen;
        var tailLen = g.Length - tailStart;

        var written = headLen + tailLen;
        var result = new byte[(written & 1) != 0 ? written + 1 : written];
        Array.Copy(g, 0, result, 0, headLen);
        result[headLen - 2] = 0;
        result[headLen - 1] = 0;
        Array.Copy(g, tailStart, result, headLen, tailLen);
        return result;
    }
}
