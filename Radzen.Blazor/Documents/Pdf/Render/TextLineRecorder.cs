using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Geometry;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf.Render;

internal sealed class TextLineRecorder(
    FontRegistry fontRegistry,
    ImageRegistry imageRegistry,
    StructureTreeBuilder structureTree,
    UnsupportedCharacterPolicy unsupportedCharacters,
    UnsupportedCharacterLog unsupported,
    bool embedFieldAppearances)
{
    private readonly GlyphSpanRecorder spans = new(fontRegistry, unsupportedCharacters, unsupported);

    public void EmitLines(
        PageRenderContext context,
        ImmutableArray<LaidOutLine> lines,
        double left,
        double baseTop,
        double delta,
        double opacity,
        StructureElement? inherited,
        bool resolveStructure,
        PdfRect? clip,
        SemanticArtifactKind? artifact = null)
    {
        foreach (var current in lines)
        {
            var element = resolveStructure ? structureTree.ElementOf(current.Source) ?? inherited : null;
            var lineArtifact = artifact ?? structureTree.ArtifactOf(current.Source);
            var marker = resolveStructure ? structureTree.MarkerElementOf(current.Source) : null;
            var box = current.Line;
            EmitLine(
                context,
                box,
                left + current.X,
                BottomUpSpace.FromTop(baseTop, current.Y + delta),
                element,
                opacity,
                marker,
                lineArtifact,
                resolveFragments: resolveStructure,
                clip: clip);
        }
    }

    public void EmitLine(
        PageRenderContext context,
        LineBox line,
        double originX,
        double baseline,
        StructureElement? element,
        double opacity = 1,
        StructureElement? markerElement = null,
        SemanticArtifactKind? artifact = null,
        bool resolveFragments = false,
        PdfRect? clip = null)
    {
        var plan = context.Plan;
        var y = baseline - line.Baseline;
        foreach (var run in line.ShapedRuns)
        {
            var alpha = opacity * run.Paint.Opacity;
            var fragElement = run.IsMarker && markerElement is not null ? markerElement : element;
            var captured = resolveFragments ? structureTree.ElementOf(run.Source) : null;
            var capturedArtifact = resolveFragments ? structureTree.ArtifactOf(run.Source) : null;
            if (run.Paint.InlineImage is { } inlineImage)
            {
                var imageElement = captured
                    ?? (capturedArtifact is not null && structureTree.TaggingActive ? null : fragElement);
                EmitInlineImage(
                    plan,
                    inlineImage,
                    originX + run.XOffset,
                    y,
                    imageElement,
                    imageElement is null ? capturedArtifact ?? artifact : null,
                    alpha);
                continue;
            }

            if (run.Paint.FormField is { } field)
            {
                plan.Widgets.Add(new WidgetDraw
                {
                    Sequence = plan.NextSequence(),
                    X = originX + run.XOffset,
                    Bottom = y - (field.Height - field.Ascent),
                    Field = field,
                    Font = run.Paint.Font,
                    Element = captured,
                    Appearance = FieldAppearance(plan, field, run.Paint.Font),
                });
                continue;
            }

            fragElement = captured ?? fragElement;

            var extGState = alpha < 1 ? plan.Resources.RegisterExtGState(alpha, alpha) : null;
            EmitGlyphRun(
                plan,
                run.Paint,
                run.GlyphRun,
                originX + run.XOffset,
                y,
                fragElement,
                capturedArtifact ?? artifact,
                extGState,
                clip);
        }

        var decorationArtifact = SemanticArtifacts.ForDecoration(artifact);
        EmitDecorations(plan, line, originX, y, opacity, decorationArtifact, underline: true);
        EmitDecorations(plan, line, originX, y, opacity, decorationArtifact, underline: false);
    }

    // ISO 19005-2 6.2.11.4.1: every font used to render text shall be embedded, and a widget appearance
    // stream renders the field value.
    private ImmutableArray<EmittedWidgetSpan> FieldAppearance(
        PagePlan plan,
        in FormFieldPaint field,
        in FontPaint font)
    {
        if (!embedFieldAppearances || field.ValueGlyphs.Spans.IsDefaultOrEmpty)
        {
            return default;
        }

        var built = ImmutableArray.CreateBuilder<EmittedWidgetSpan>(field.ValueGlyphs.Spans.Length);
        foreach (var span in field.ValueGlyphs.Spans)
        {
            var emitted = spans.Emit(span, font.Size);
            plan.Resources.UsedFonts.Add(emitted.Font);
            built.Add(new EmittedWidgetSpan(emitted.Font, emitted.Bytes, span.XOffset));
        }

        return built.MoveToImmutable();
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

    private static void AddDecorationEdge(
        PagePlan plan,
        double x1,
        double x2,
        double edgeY,
        double size,
        Color color,
        double alpha,
        SemanticArtifactKind artifact)
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
            Artifact = artifact,
            ExtGState = alpha < 1 ? plan.Resources.RegisterExtGState(alpha, alpha) : null,
        });
    }

    private static void EmitDecorations(
        PagePlan plan,
        LineBox line,
        double originX,
        double y,
        double opacity,
        SemanticArtifactKind artifact,
        bool underline)
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
                opacity * fragment.Paint.Opacity,
                artifact);
        }
    }

    private static TextDraw BuildTextDraw(
        in FragmentPaint paint,
        double x,
        double baseline,
        double size,
        EmittedFont generated,
        byte[] bytes,
        StructureElement? element,
        SemanticArtifactKind? artifact,
        string? extGState,
        double[]? kerns,
        int sequence,
        PdfRect? clip,
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
            Artifact = artifact,
            Kerns = kerns,
            Clip = clip,
        };

    private void EmitGlyphRun(
        PagePlan plan,
        in FragmentPaint paint,
        in CapturedGlyphRun glyphRun,
        double startX,
        double y,
        StructureElement? element,
        SemanticArtifactKind? artifact,
        string? extGState,
        PdfRect? clip)
    {
        var font = paint.Font;
        var size = font.Size * paint.ScriptScale;
        foreach (var span in glyphRun.Spans)
        {
            var x = startX + span.XOffset;
            var emitted = spans.Emit(span, font.Size);
            plan.Resources.UsedFonts.Add(emitted.Font);
            plan.Texts.Add(BuildTextDraw(
                paint,
                x,
                y,
                size,
                emitted.Font,
                emitted.Bytes,
                element,
                artifact,
                extGState,
                emitted.Kerns,
                plan.NextSequence(),
                clip,
                emitted.Face is { } face && font.Bold && !face.Bold ? size * 0.03 : 0,
                emitted.Face is { } italicFace && font.Italic && !italicFace.Italic ? 0.21 : 0));
        }
    }

    private void EmitInlineImage(
        PagePlan plan,
        in InlineImagePaint image,
        double x,
        double baseline,
        StructureElement? element,
        SemanticArtifactKind? artifact,
        double alpha)
    {
        var generated = imageRegistry.DecodeBytes(image.Key, image.Data);
        plan.Images.Add(new ImageDraw
        {
            Sequence = plan.NextSequence(),
            X = x,
            Y = baseline,
            Width = image.Width,
            Height = image.Height,
            Image = generated,
            Element = element,
            Artifact = artifact,
            ExtGState = alpha < 1 ? plan.Resources.RegisterExtGState(alpha, alpha) : null,
        });
        plan.Resources.UsedImages.Add(generated);
    }
}
