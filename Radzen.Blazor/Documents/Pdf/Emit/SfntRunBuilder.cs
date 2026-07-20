using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Emit;

internal readonly struct SfntGlyphRun
{
    public required SfntFont Face { get; init; }

    public required GeneratedFont Font { get; init; }

    public required byte[] Bytes { get; init; }

    public required double[]? Kerns { get; init; }

    public required double Advance { get; init; }

    public int GlyphCount => Bytes.Length / 2;

    public required int WordSpaceCount { get; init; }
}

internal sealed class SfntRunBuilder(FontCollection fonts, GeneratorFontResolver fontResolver)
{
    private readonly List<SfntGlyphRun> runs = [];
    private readonly List<byte> scratchBytes = [];

    public IReadOnlyList<SfntGlyphRun> Build(string text, Font font, double size)
    {
        runs.Clear();
        var glyphs = fonts.Shaper().Shape(text, font, out _);
        var g = 0;
        while (g < glyphs.Count)
        {
            var face = glyphs[g].Face;
            var generated = fontResolver.ResolveSfnt(face);
            var run = new SfntRunAccumulator(face, generated, size, fonts.EnableKerning, scratchBytes);
            run.Begin();
            while (g < glyphs.Count && ReferenceEquals(glyphs[g].Face, face))
            {
                run.Append(glyphs[g], FontCollection.CodePointAt(text, glyphs[g].Cluster));
                g++;
            }

            runs.Add(run.ToRun());
        }

        return runs;
    }

    internal static void AppendGlyph(GeneratedFont font, List<byte> bytes, ushort gid, int codepoint)
    {
        font.GidToUnicode.TryAdd(gid, codepoint);
        bytes.Add((byte)(gid >> 8));
        bytes.Add((byte)(gid & 0xFF));
    }

    internal static bool HasNonZero(List<double>? values)
    {
        if (values is null)
        {
            return false;
        }

        foreach (var value in values)
        {
            if (value != 0)
            {
                return true;
            }
        }

        return false;
    }
}

internal struct SfntRunAccumulator(
    SfntFont face,
    GeneratedFont font,
    double size,
    bool kerning,
    List<byte> bytes)
{
    private List<double>? kernList = null;
    private ushort previousGid = 0;
    private int previousCodepoint = 0;
    private double previousShapedAdvance = 0;
    private int glyphCount = 0;
    private int wordSpaceCount = 0;

    public double Advance { get; private set; } = 0;

    public int GlyphCount => glyphCount;

    public int WordSpaceCount => wordSpaceCount;

    public byte[] Bytes => [.. bytes];

    public double[]? Kerns => SfntRunBuilder.HasNonZero(kernList) ? [.. kernList!] : null;

    public void Begin() => bytes.Clear();

    public void Append(in PositionedGlyph glyph, int codepoint)
    {
        if (glyphCount > 0)
        {
            AddKern(SimpleShaper.TrailingKerning(face, previousGid, previousShapedAdvance, size));
        }

        SfntRunBuilder.AppendGlyph(font, bytes, glyph.GlyphId, codepoint);
        Advance += glyph.Advance;
        previousGid = glyph.GlyphId;
        previousCodepoint = codepoint;
        previousShapedAdvance = glyph.Advance;
        CountGlyph(codepoint);
    }

    public void Append(ushort gid, int codepoint)
    {
        if (kerning && glyphCount > 0)
        {
            var kern = SimpleShaper.PairKerning(
                face, previousGid, gid, previousCodepoint, codepoint, size);
            Advance += kern;
            AddKern(kern);
        }

        SfntRunBuilder.AppendGlyph(font, bytes, gid, codepoint);
        Advance += face.AdvanceInUserSpace(gid, size);
        previousGid = gid;
        previousCodepoint = codepoint;
        CountGlyph(codepoint);
    }

    private void AddKern(double kern)
        => (kernList ??= []).Add(size == 0 ? 0 : -FontMetric.Scale(kern, 1000, size));

    private void CountGlyph(int codepoint)
    {
        glyphCount++;
        if (codepoint == ' ')
        {
            wordSpaceCount++;
        }
    }

    public SfntGlyphRun ToRun() => new()
    {
        Face = face,
        Font = font,
        Bytes = Bytes,
        Kerns = Kerns,
        Advance = Advance,
        WordSpaceCount = wordSpaceCount,
    };
}
