using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf;

// A minimal left-to-right, horizontal shaper: identity cmap mapping with per-character
// fallback, hmtx advances, optional legacy 'kern' pairs. Kerning is off by default so shaped
// advances match FontCollection.MeasureText unless asked for. Both the measure (FontCollection)
// and emit (TextLineEmitter) paths shape through this one helper, so what is measured is drawn.
internal sealed class SimpleShaper(FontCollection fonts, bool enableKerning = false)
{
    // Implemented by structs and consumed through a struct generic constraint so the JIT
    // devirtualizes every call; boxing this into an interface field would undo that.
    private interface IGlyphSink
    {
        void Add(ushort glyph, double advance, int cluster, SfntFont face);

        void Kern(double kern);
    }

    private struct MeasureSink : IGlyphSink
    {
        public double Total;

        public void Add(ushort glyph, double advance, int cluster, SfntFont face) => Total += advance;

        public void Kern(double kern) => Total += kern;
    }

    private struct CollectSink : IGlyphSink
    {
        public List<PositionedGlyph> Glyphs;
        public double Total;

        public void Add(ushort glyph, double advance, int cluster, SfntFont face)
        {
            Glyphs.Add(new PositionedGlyph(glyph, advance, cluster, face));
            Total += advance;
        }

        public void Kern(double kern)
        {
            var last = Glyphs.Count - 1;
            var previous = Glyphs[last];
            Glyphs[last] = new PositionedGlyph(previous.GlyphId, previous.Advance + kern, previous.Cluster, previous.Face);
            Total += kern;
        }
    }

    // The one shaping loop: measure and emit differ only in the sink, so a kern or fallback
    // change cannot shift a measured line relative to its drawn glyphs.
    private void ShapeCore<TSink>(ReadOnlySpan<char> text, Font font, ref TSink sink)
        where TSink : struct, IGlyphSink
    {
        ArgumentNullException.ThrowIfNull(font);

        EnsureNoComplexScript(text);

        var primary = fonts.ResolvePrimarySfnt(font);

        SfntFont? previousFace = null;
        ushort previousGlyph = 0;
        var count = 0;
        var i = 0;
        while (i < text.Length)
        {
            var codepoint = FontCollection.CodePointAt(text, i);
            var (face, glyph) = fonts.ResolveGlyph(primary, codepoint);
            var advance = face.GetAdvanceWidth(glyph) * font.Size / face.UnitsPerEm;

            // Kerning adjusts the space between the previous glyph and this one, so it is
            // folded into the previous glyph's advance. It applies only within a single face.
            if (enableKerning && ReferenceEquals(previousFace, face) && count > 0)
            {
                var kern = previousFace!.GetKerning(previousGlyph, glyph) * font.Size / previousFace.UnitsPerEm;
                if (kern != 0)
                {
                    sink.Kern(kern);
                }
            }

            sink.Add(glyph, advance, i, face);
            count++;
            previousFace = face;
            previousGlyph = glyph;
            i += codepoint > 0xFFFF ? 2 : 1;
        }
    }

    // totalAdvance always equals the sum of the returned glyphs' advances, since a kern is
    // folded into both.
    public List<PositionedGlyph> Shape(ReadOnlySpan<char> text, Font font, out double totalAdvance)
    {
        var sink = new CollectSink { Glyphs = new List<PositionedGlyph>(text.Length) };
        ShapeCore(text, font, ref sink);
        totalAdvance = sink.Total;
        return sink.Glyphs;
    }

    public double MeasureAdvance(ReadOnlySpan<char> text, Font font)
    {
        var sink = new MeasureSink();
        ShapeCore(text, font, ref sink);
        return sink.Total;
    }

    // Shared by the shaper and by text measuring, so both refuse the same text.
    internal static void EnsureNoComplexScript(ReadOnlySpan<char> text)
    {
        var i = 0;
        while (i < text.Length)
        {
            var codepoint = FontCollection.CodePointAt(text, i);
            if (RequiresComplexShaping(codepoint))
            {
                throw new NotSupportedException(
                    $"The text contains U+{codepoint:X4}, which belongs to a complex or right-to-left script (Arabic, Hebrew, "
                    + "Syriac, Thaana, N'Ko, Samaritan, Indic, Thai/Lao, Tibetan, Myanmar, Khmer, Mongolian, Adlam or another "
                    + "script), or is a bidirectional control, that requires shaping (joining/reordering) or bidirectional "
                    + "reordering. This library would emit unshaped, unjoined or visually reversed glyphs, so it fails rather "
                    + "than produce linguistically broken output. Provide pre-shaped glyph runs if you need these scripts.");
            }

            i += codepoint > 0xFFFF ? 2 : 1;
        }
    }

    // Scripts the identity LTR mapper would render unjoined or visually reversed. Tested
    // against code points, not UTF-16 code units, so supplementary-plane scripts are caught.
    // Latin/Cyrillic/Greek/CJK/Hangul are deliberately absent - they need no shaping.
    private static bool RequiresComplexShaping(int c)
        => c is (>= 0x0590 and <= 0x05FF)  // Hebrew (RTL)
            or (>= 0x0600 and <= 0x06FF)    // Arabic
            or (>= 0x0700 and <= 0x074F)    // Syriac
            or (>= 0x0750 and <= 0x077F)    // Arabic Supplement
            or (>= 0x0780 and <= 0x07BF)    // Thaana (RTL)
            or (>= 0x07C0 and <= 0x07FF)    // N'Ko (RTL)
            or (>= 0x0800 and <= 0x085F)    // Samaritan, Mandaic (RTL)
            or (>= 0x0860 and <= 0x086F)    // Syriac Supplement
            or (>= 0x0870 and <= 0x089F)    // Arabic Extended-B
            or (>= 0x08A0 and <= 0x08FF)    // Arabic Extended-A
            or (>= 0x0900 and <= 0x0DFF)    // Devanagari .. Malayalam / Sinhala (Indic)
            or (>= 0x0E00 and <= 0x0EFF)    // Thai, Lao
            or (>= 0x0F00 and <= 0x0FFF)    // Tibetan
            or (>= 0x1000 and <= 0x109F)    // Myanmar
            or (>= 0x1780 and <= 0x17FF)    // Khmer
            or (>= 0x1800 and <= 0x18AF)    // Mongolian
            or 0x200F                       // RIGHT-TO-LEFT MARK
            or (>= 0x202A and <= 0x202E)    // LRE/RLE/PDF/LRO/RLO embedding + override controls
            or (>= 0x2066 and <= 0x2069)    // LRI/RLI/FSI/PDI isolate controls
            or (>= 0xFB1D and <= 0xFB4F)    // Hebrew Presentation Forms (RTL)
            or (>= 0xFB50 and <= 0xFDFF)    // Arabic Presentation Forms-A
            or (>= 0xFE70 and <= 0xFEFF)    // Arabic Presentation Forms-B
            or (>= 0x10A00 and <= 0x10A5F)  // Kharoshthi (RTL)
            or (>= 0x10AC0 and <= 0x10AFF)  // Manichaean (RTL)
            or (>= 0x10B80 and <= 0x10BAF)  // Psalter Pahlavi (RTL)
            or (>= 0x10D00 and <= 0x10D3F)  // Hanifi Rohingya (RTL)
            or (>= 0x10E80 and <= 0x10EFF)  // Yezidi, Arabic Extended-C (RTL)
            or (>= 0x10F00 and <= 0x10F6F)  // Sogdian, Old Sogdian (RTL)
            or (>= 0x11000 and <= 0x11FFF)  // Brahmic supplementary (Brahmi, Kaithi, Chakma, ...)
            or (>= 0x1E800 and <= 0x1E8DF)  // Mende Kikakui (RTL)
            or (>= 0x1E900 and <= 0x1E95F)  // Adlam (RTL)
            or (>= 0x1EE00 and <= 0x1EEFF); // Arabic Mathematical Alphabetic Symbols (RTL)
}
