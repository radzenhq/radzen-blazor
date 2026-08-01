using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal static partial class LineLayouter
{
    private static (double Width, double DecimalOffset) MeasureSegment(
        Span<LineFragment> span,
        List<LineWord> words,
        int wordStart,
        int wordEnd,
        int fragmentStart,
        FontCollection fonts)
    {
        double width = 0;
        double decimalOffset = -1;
        var fragmentIndex = fragmentStart;
        for (var word = wordStart; word <= wordEnd; word++)
        {
            for (var piece = 0; piece < words[word].PieceCount; piece++)
            {
                var fragment = span[fragmentIndex];
                if (decimalOffset < 0)
                {
                    var dot = fragment.Text.IndexOf('.', StringComparison.Ordinal);
                    if (dot >= 0)
                    {
                        decimalOffset = width + fonts.MeasureText(fragment.Text[..dot], fragment.Paint.Font);
                    }
                }

                width += fragment.Advance;
                fragmentIndex++;
            }

            if (word < wordEnd)
            {
                width += words[word].GapAfter;
            }
        }

        return (width, decimalOffset < 0 ? width : decimalOffset);
    }

    private static (double Height, double Baseline) Measure(
        ImmutableArray<LineFragment> fragments,
        double lineSpacing,
        FontCollection fonts)
    {
        double natural = 0;
        double baseline = 0;
        for (var index = 0; index < fragments.Length; index++)
        {
            var paint = fragments[index].Paint;
            var (height, ascent) = paint.InlineImage is { } image
                ? (image.Height, image.Height)
                : FontExtent(paint.Font, fonts);
            if (paint.IsScript)
            {
                (height, ascent) = ScriptExtent(paint, height, ascent);
            }

            natural = Math.Max(natural, height);
            baseline = Math.Max(baseline, ascent);
        }

        return (natural * lineSpacing, baseline);
    }

    private static (double Height, double Ascent) ScriptExtent(in FragmentPaint paint, double height, double ascent)
    {
        var scale = paint.ScriptScale;
        var rise = paint.Rise;
        var descent = (height - ascent) * scale;
        ascent = (ascent * scale) + Math.Max(rise, 0);
        return (ascent + descent + Math.Max(-rise, 0), ascent);
    }

    private static (double Height, double Ascent) FontExtent(in FontPaint font, FontCollection fonts)
    {
        var size = font.Size;
        if (fonts.TryResolvePrimary(font, out var face))
        {
            var unitsPerEm = face.UnitsPerEm;
            return (
                (face.Ascent - face.Descent + face.LineGap) * size / unitsPerEm,
                face.Ascent * size / unitsPerEm);
        }

        if (BuiltInFontMetrics.Resolve(BuiltInPaint(font)) is { } builtIn)
        {
            var metrics = builtIn.Face().Metrics;
            var unitsPerEm = metrics.DesignUnitsPerEm;
            return (
                (metrics.BBoxTop - metrics.BBoxBottom) * size / unitsPerEm,
                metrics.BBoxTop * size / unitsPerEm);
        }

        return (size * 1.2, size * 0.9);
    }

    private static FontPaint BuiltInPaint(in FontPaint font)
        => string.IsNullOrEmpty(font.Family) ? font with { Family = Font.DefaultFamily } : font;

    private readonly record struct LinePlacement(double Width, HyphenPlacement Hyphen);

    private readonly record struct HyphenPlacement(bool Include, double XOffset, FontPaint Font, double Width);
}
