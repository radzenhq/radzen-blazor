using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf;

internal sealed class SimpleShaper(FontCollection fonts, bool enableKerning = false)
{
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

    private void ShapeCore<TSink>(ReadOnlySpan<char> text, Font font, ref TSink sink)
        where TSink : struct, IGlyphSink
    {
        EnsureNoComplexScript(text);

        var primary = fonts.ResolvePrimarySfnt(font);

        SfntFont? previousFace = null;
        ushort previousGlyph = 0;
        var previousCodepoint = 0;
        var count = 0;
        var i = 0;
        while (i < text.Length)
        {
            var codepoint = FontCollection.CodePointAt(text, i, out var codePointLength);
            var (face, glyph) = fonts.ResolveGlyph(primary, codepoint);
            var advance = face.AdvanceInUserSpace(glyph, font.Size);

            if (enableKerning && ReferenceEquals(previousFace, face) && count > 0)
            {
                var kern = PairKerning(previousFace!, previousGlyph, glyph, previousCodepoint, codepoint, font.Size);
                if (kern != 0)
                {
                    sink.Kern(kern);
                }
            }

            sink.Add(glyph, advance, i, face);
            count++;
            previousFace = face;
            previousGlyph = glyph;
            previousCodepoint = codepoint;
            i += codePointLength;
        }
    }

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

    internal static double PairKerning(
        SfntFont face, ushort left, ushort right, int leftCodepoint, int rightCodepoint, double size)
        => leftCodepoint == ' ' || rightCodepoint == ' '
            ? 0
            : face.KerningInUserSpace(left, right, size);

    internal static double TrailingKerning(
        SfntFont face, ushort glyph, double shapedAdvance, double size)
        => shapedAdvance - face.AdvanceInUserSpace(glyph, size);

    internal static void EnsureNoComplexScript(ReadOnlySpan<char> text)
    {
        var i = 0;
        while (i < text.Length)
        {
            var codepoint = FontCollection.CodePointAt(text, i, out var codePointLength);
            if (RequiresComplexShaping(codepoint))
            {
                throw new NotSupportedException(
                    $"The text contains U+{codepoint:X4}, which belongs to a complex or right-to-left script (Arabic, Hebrew, "
                    + "Syriac, Thaana, N'Ko, Samaritan, Indic, Thai/Lao, Tibetan, Myanmar, Khmer, Mongolian, Adlam or another "
                    + "script), or is a bidirectional control, that requires shaping (joining/reordering) or bidirectional "
                    + "reordering. This library would emit unshaped, unjoined or visually reversed glyphs, so it fails rather "
                    + "than produce linguistically broken output. Provide pre-shaped glyph runs if you need these scripts.");
            }

            i += codePointLength;
        }
    }

    private static bool RequiresComplexShaping(int c)
        => c is (>= 0x0590 and <= 0x05FF)
            or (>= 0x0600 and <= 0x06FF)
            or (>= 0x0700 and <= 0x074F)
            or (>= 0x0750 and <= 0x077F)
            or (>= 0x0780 and <= 0x07BF)
            or (>= 0x07C0 and <= 0x07FF)
            or (>= 0x0800 and <= 0x085F)
            or (>= 0x0860 and <= 0x086F)
            or (>= 0x0870 and <= 0x089F)
            or (>= 0x08A0 and <= 0x08FF)
            or (>= 0x0900 and <= 0x0DFF)
            or (>= 0x0E00 and <= 0x0EFF)
            or (>= 0x0F00 and <= 0x0FFF)
            or (>= 0x1000 and <= 0x109F)
            or (>= 0x1780 and <= 0x17FF)
            or (>= 0x1800 and <= 0x18AF)
            or 0x200F
            or (>= 0x202A and <= 0x202E)
            or (>= 0x2066 and <= 0x2069)
            or (>= 0xFB1D and <= 0xFB4F)
            or (>= 0xFB50 and <= 0xFDFF)
            or (>= 0xFE70 and <= 0xFEFF)
            or (>= 0x10A00 and <= 0x10A5F)
            or (>= 0x10AC0 and <= 0x10AFF)
            or (>= 0x10B80 and <= 0x10BAF)
            or (>= 0x10D00 and <= 0x10D3F)
            or (>= 0x10E80 and <= 0x10EFF)
            or (>= 0x10F00 and <= 0x10F6F)
            or (>= 0x11000 and <= 0x11FFF)
            or (>= 0x1E800 and <= 0x1E8DF)
            or (>= 0x1E900 and <= 0x1E95F)
            or (>= 0x1EE00 and <= 0x1EEFF);
}
