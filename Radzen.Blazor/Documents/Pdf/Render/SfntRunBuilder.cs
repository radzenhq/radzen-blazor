using System.Collections.Generic;
using System;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf.Render;

internal readonly struct SfntGlyphRun
{
    public required SfntFont Face { get; init; }

    public required EmittedFont Font { get; init; }

    public required byte[] Bytes { get; init; }

    public required double[]? Kerns { get; init; }

    public required double Advance { get; init; }

}

internal sealed class SfntRunBuilder(GeneratorFontResolver fontResolver)
{
    public SfntGlyphRun Build(in CapturedGlyphSpan span, double emSize)
    {
        if (!span.IsSfnt)
        {
            throw new InvalidOperationException("An sfnt run requires a captured sfnt face.");
        }

        var face = span.Face.Sfnt;
        var generated = fontResolver.ResolveSfnt(face);
        var glyphs = span.SfntGlyphs;
        var bytes = new byte[glyphs.Length * 2];
        var kerns = glyphs.Length > 1 ? new double[glyphs.Length - 1] : [];
        for (var i = 0; i < glyphs.Length; i++)
        {
            var glyph = glyphs[i];
            generated.GidToUnicode.TryAdd(glyph.GlyphId, glyph.Codepoint);
            bytes[i * 2] = (byte)(glyph.GlyphId >> 8);
            bytes[i * 2 + 1] = (byte)(glyph.GlyphId & 0xFF);
            if (i < kerns.Length)
            {
                kerns[i] = PdfTextAdjustment(glyph.TextAdjustmentPoints, emSize);
            }
        }

        return new SfntGlyphRun
        {
            Face = face,
            Font = generated,
            Bytes = bytes,
            Kerns = HasNonZero(kerns) ? kerns : null,
            Advance = span.Advance,
        };
    }

    public IReadOnlyList<SfntGlyphRun> Build(in CapturedGlyphRun captured, double emSize)
    {
        var runs = new List<SfntGlyphRun>(captured.Spans.Length);
        foreach (var span in captured.Spans)
        {
            if (!span.IsSfnt)
            {
                throw new InvalidOperationException("A base-14 span cannot be built as an sfnt run.");
            }

            runs.Add(Build(span, emSize));
        }

        return runs;
    }

    internal static bool HasNonZero(IReadOnlyList<double> values)
    {
        foreach (var value in values)
        {
            if (value != 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static double PdfTextAdjustment(double adjustmentPoints, double emSize)
        => emSize == 0 ? 0 : FontMetric.Scale(adjustmentPoints, 1000, emSize);
}
