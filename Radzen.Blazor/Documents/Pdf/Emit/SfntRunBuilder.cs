using System.Collections.Generic;
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
}

internal sealed class SfntRunBuilder(FontCollection fonts, GeneratorFontResolver fontResolver)
{
    private readonly List<SfntGlyphRun> runs = [];
    private readonly List<byte> scratchBytes = [];

    public IReadOnlyList<SfntGlyphRun> Build(string text, Font font, double size, bool kernAcrossSpaces)
    {
        runs.Clear();
        var kerning = fonts.EnableKerning;
        var glyphs = fonts.Shaper().Shape(text, font, out _);
        var g = 0;
        while (g < glyphs.Count)
        {
            var face = glyphs[g].Face;
            var generated = fontResolver.ResolveSfnt(face);
            scratchBytes.Clear();
            var advance = 0.0;
            List<double>? kernList = null;
            ushort previousGid = 0;
            var previousCodepoint = 0;
            var glyphCount = 0;
            while (g < glyphs.Count && ReferenceEquals(glyphs[g].Face, face))
            {
                var gid = glyphs[g].GlyphId;
                var codepoint = FontCollection.CodePointAt(text, glyphs[g].Cluster);

                if (kerning && glyphCount > 0)
                {
                    var straddlesSpace = codepoint == ' ' || previousCodepoint == ' ';
                    var kern = straddlesSpace && !kernAcrossSpaces ? 0 : face.GetKerning(previousGid, gid);
                    advance += kern * size / face.UnitsPerEm;
                    (kernList ??= []).Add(-kern * 1000.0 / face.UnitsPerEm);
                }

                advance += AppendGlyph(generated, scratchBytes, face, gid, codepoint, size);
                previousGid = gid;
                previousCodepoint = codepoint;
                glyphCount++;
                g++;
            }

            runs.Add(new SfntGlyphRun
            {
                Face = face,
                Font = generated,
                Bytes = [.. scratchBytes],
                Kerns = HasNonZero(kernList) ? [.. kernList!] : null,
                Advance = advance,
            });
        }

        return runs;
    }

    internal static double AppendGlyph(GeneratedFont font, List<byte> bytes, SfntFont face, ushort gid, int codepoint, double size)
    {
        font.GidToUnicode.TryAdd(gid, codepoint);
        bytes.Add((byte)(gid >> 8));
        bytes.Add((byte)(gid & 0xFF));
        return face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
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
