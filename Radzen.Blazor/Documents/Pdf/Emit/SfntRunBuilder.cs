using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Emit;

// One drawable glyph run: the maximal stretch of shaped text supplied by a single physical
// face, encoded as 2-byte GIDs with the TJ kern adjustments and the advance it occupies.
internal readonly struct SfntGlyphRun
{
    public required SfntFont Face { get; init; }

    public required GeneratedFont Font { get; init; }

    public required byte[] Bytes { get; init; }

    public required double[]? Kerns { get; init; }

    public required double Advance { get; init; }

    public int GlyphCount => Bytes.Length / 2;
}

// Builds the embedded-font glyph runs every sfnt draw site shows. Text is mapped through
// the same FontCollection.Shaper() that measured it, so the drawn glyphs are the measured
// glyphs and the emitted kerns are the kerns measurement summed - a run can never be
// positioned by one width and painted at another.
internal sealed class SfntRunBuilder(FontCollection fonts, GeneratorFontResolver fontResolver)
{
    private readonly List<SfntGlyphRun> runs = [];
    private readonly List<byte> scratchBytes = [];

    // Returns the runs for text, reusing one list per builder: consume them before the next call.
    // kernAcrossSpaces mirrors how the caller measured the text. A line coalesces separately
    // measured words across their spaces, so layout never saw a space-straddling pair and kerning
    // it here would drift the line; text measured whole (a watermark) did see those pairs.
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
                    // One entry per glyph gap (0 when the pair is not kerned) so the TJ
                    // adjustments stay aligned with the glyph codes. Measurement adds
                    // kern*size/upem; the TJ number (-kern*1000/upem, positive tightens)
                    // reproduces the same displacement when drawn.
                    var straddlesSpace = codepoint == ' ' || previousCodepoint == ' ';
                    var kern = straddlesSpace && !kernAcrossSpaces ? 0 : face.GetKerning(previousGid, gid);
                    advance += kern * size / face.UnitsPerEm;
                    (kernList ??= []).Add(-kern * 1000.0 / face.UnitsPerEm);
                }

                // First-seen codepoint wins so glyphs shared by several codepoints
                // (e.g. hyphen/soft-hyphen) map deterministically, not by draw order.
                generated.GidToUnicode.TryAdd(gid, codepoint);
                scratchBytes.Add((byte)(gid >> 8));
                scratchBytes.Add((byte)(gid & 0xFF));
                advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
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

    private static bool HasNonZero(List<double>? values)
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
