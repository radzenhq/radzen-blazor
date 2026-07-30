using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class TextLineEmitter(
    GeneratorFontResolver fontResolver,
    ImageStore imageStore,
    StructureTreeBuilder structureTree,
    bool allowUnsupportedCharacters)
{
    private readonly SfntRunBuilder runBuilder = new(fontResolver);
    private readonly Base14GlyphEncoder base14Encoder = new(allowUnsupportedCharacters);

    public void EmitBandLines(
        EmitContext context,
        ImmutableArray<PositionedLine> lines,
        double left,
        double top)
        => EmitLines(
            context, lines,
            static l => l.Line, static l => l.Source, static _ => 0, static l => l.Y,
            left, top, delta: 0,
            opacity: 1, inherited: null, resolveStructure: false,
            overflowThreshold: double.PositiveInfinity);

    public bool EmitLines<TLine>(
        EmitContext context,
        ImmutableArray<TLine> lines,
        Func<TLine, LineBox> lineOf,
        Func<TLine, SourceId> sourceOf,
        Func<TLine, double> xOf,
        Func<TLine, double> yOf,
        double left,
        double baseTop,
        double delta,
        double opacity,
        StructureElement? inherited,
        bool resolveStructure,
        double overflowThreshold)
    {
        var overflows = false;
        foreach (var current in lines)
        {
            var source = sourceOf(current);
            var element = resolveStructure ? structureTree.ElementOf(source) ?? inherited : null;
            var marker = resolveStructure ? structureTree.MarkerElementOf(source) : null;
            var box = lineOf(current);
            EmitLine(
                context, box, left + xOf(current), baseTop - (yOf(current) + delta), element, opacity, marker,
                resolveFragments: resolveStructure);
            overflows |= box.Width > overflowThreshold + 0.01;
        }

        return overflows;
    }

    public void EmitLine(
        EmitContext context,
        LineBox line,
        double originX,
        double baseline,
        StructureElement? element,
        double opacity = 1,
        StructureElement? markerElement = null,
        bool resolveFragments = false)
    {
        var plan = context.Plan;
        var y = baseline - line.Baseline;
        for (var fi = 0; fi < line.Fragments.Length; fi++)
        {
            var fragment = line.Fragments[fi];
            var alpha = opacity * fragment.Paint.Opacity;
            var fragElement = fragment.IsMarker && markerElement is not null ? markerElement : element;
            var captured = resolveFragments ? structureTree.ElementOf(fragment.Source) : null;
            if (fragment.Paint.InlineImage is { } inlineImage)
            {
                EmitInlineImage(
                    plan,
                    inlineImage,
                    originX + fragment.XOffset,
                    y,
                    captured ?? (structureTree.TaggingActive ? null : fragElement),
                    alpha);
                continue;
            }

            fragElement = captured ?? fragElement;

            if (fragment.SuppressTextEmission)
            {
                continue;
            }

            var glyphRun = fragment.CoalescedGlyphRun ?? fragment.GlyphRun;
            if (glyphRun.Spans.IsDefaultOrEmpty)
            {
                continue;
            }

            var extGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null;
            EmitGlyphRun(
                plan,
                fragment,
                glyphRun,
                originX + fragment.XOffset,
                y,
                fragElement,
                extGState);
        }

        EmitDecorations(plan, line, originX, y, opacity, underline: true);
        EmitDecorations(plan, line, originX, y, opacity, underline: false);
    }

    private static IEnumerable<(int First, double Start, double End)> Spans(
        ImmutableArray<LineFragment> fragments,
        Func<LineFragment, bool> eligible,
        Func<LineFragment, LineFragment, bool> sameGroup)
    {
        var i = 0;
        while (i < fragments.Length)
        {
            var first = fragments[i];
            if (!eligible(first))
            {
                i++;
                continue;
            }

            var start = first.XOffset;
            var end = start + first.Advance;
            var j = i + 1;
            while (j < fragments.Length && sameGroup(first, fragments[j]))
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            yield return (i, start, end);
            i = j;
        }
    }

    private static double DecorationThickness(double size) => Math.Max(size * 0.06, 0.5);

    private static void AddDecorationEdge(PagePlan plan, double x1, double x2, double edgeY, double size, Color color, double alpha)
    {
        plan.Edges.Add(new EdgeDraw
        {
            X1 = x1,
            Y1 = edgeY,
            X2 = x2,
            Y2 = edgeY,
            LineWidth = DecorationThickness(size),
            Color = color,
            Style = BorderStyle.Solid,
            ExtGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null,
        });
    }

    private static void EmitDecorations(
        PagePlan plan, LineBox line, double originX, double y, double opacity, bool underline)
    {
        foreach (var (first, start, end) in Spans(
            line.Fragments,
            f => (underline ? f.Paint.Font.Underline : f.Paint.Font.Strikethrough) && f.Text.Length > 0,
            (f, next) => underline
                ? next.Source == f.Source
                : next.Paint.Font.Strikethrough
                    && next.Paint.Font.Size == f.Paint.Font.Size
                    && next.Paint.Font.Color.Equals(f.Paint.Font.Color)
                    && next.Paint.Opacity == f.Paint.Opacity))
        {
            var fragment = line.Fragments[first];
            var size = fragment.Paint.Font.Size;
            AddDecorationEdge(
                plan,
                originX + start,
                originX + end,
                y + size * (underline ? -0.12 : 0.3),
                size,
                fragment.Paint.Font.Color,
                opacity * fragment.Paint.Opacity);
        }
    }

    private static TextDraw BuildTextDraw(
        in FragmentPaint paint,
        double x,
        double baseline,
        double size,
        GeneratedFont generated,
        byte[] bytes,
        StructureElement? element,
        string? extGState,
        double[]? kerns,
        int sequence,
        double strokeWidth = 0,
        double shear = 0)
        => new()
        {
            Sequence = sequence,
            X = x,
            Baseline = baseline,
            Size = size,
            Color = paint.Font.Color,
            Font = generated,
            Bytes = bytes,
            StrokeWidth = strokeWidth,
            Shear = shear,
            CharSpacing = paint.LetterSpacing,
            Rise = paint.Rise,
            WordSpacing = paint.WordSpacing,
            HorizontalScale = paint.HorizontalScale,
            RenderMode = paint.Invisible ? 3 : 0,
            ExtGState = extGState,
            Element = element,
            Kerns = kerns,
        };

    private void EmitGlyphRun(
        PagePlan plan,
        in LineFragment fragment,
        in CapturedGlyphRun glyphRun,
        double startX,
        double y,
        StructureElement? element,
        string? extGState)
    {
        var paint = fragment.Paint;
        var font = PdfModelAdapter.Materialize(paint.Font);
        var size = font.EffectiveSize.Point * paint.ScriptScale;
        foreach (var span in glyphRun.Spans)
        {
            var x = startX + span.XOffset;
            if (span.IsSfnt)
            {
                EmitSfntSpan(plan, paint, font, span, x, y, size, element, extGState);
            }
            else
            {
                EmitBase14Span(plan, paint, font, span, x, y, size, element, extGState);
            }
        }
    }

    private void EmitSfntSpan(
        PagePlan plan,
        in FragmentPaint paint,
        Font font,
        in CapturedGlyphSpan span,
        double x,
        double y,
        double size,
        StructureElement? element,
        string? extGState)
    {
        var glyphRun = runBuilder.Build(span);
        var face = glyphRun.Face;
        plan.UsedFonts.Add(glyphRun.Font);
        plan.Texts.Add(BuildTextDraw(
            paint, x, y, size, glyphRun.Font, glyphRun.Bytes, element, extGState, glyphRun.Kerns,
            plan.NextSequence(),
            font.EffectiveBold && !face.Bold ? size * 0.03 : 0,
            font.EffectiveItalic && !face.Italic ? 0.21 : 0));
    }

    private void EmitBase14Span(
        PagePlan plan,
        in FragmentPaint paint,
        Font font,
        in CapturedGlyphSpan span,
        double x,
        double y,
        double size,
        StructureElement? element,
        string? extGState)
    {
        var glyphs = span.BuiltInGlyphs;
        var bytes = base14Encoder.Encode(glyphs, font);
        var kerns = glyphs.Length > 1 ? new double[glyphs.Length - 1] : [];
        for (var i = 0; i < glyphs.Length; i++)
        {
            if (i < kerns.Length)
            {
                kerns[i] = glyphs[i].TextAdjustment;
            }
        }

        var generated = fontResolver.ResolveBase14(font);
        plan.UsedFonts.Add(generated);
        plan.Texts.Add(BuildTextDraw(
            paint,
            x,
            y,
            size,
            generated,
            bytes,
            element,
            extGState,
            SfntRunBuilder.HasNonZero(kerns) ? kerns : null,
            plan.NextSequence()));
    }

    private void EmitInlineImage(PagePlan plan, in InlineImagePaint image, double x, double baseline, StructureElement? element, double alpha)
    {
        var generated = imageStore.DecodeBytes(image.Key, image.Data);
        plan.Images.Add(new ImageDraw
        {
            Sequence = plan.NextSequence(),
            X = x,
            Y = baseline,
            Width = image.Width,
            Height = image.Height,
            Image = generated,
            Element = element,
            ExtGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null,
        });
        plan.UsedImages.Add(generated);
    }
}
