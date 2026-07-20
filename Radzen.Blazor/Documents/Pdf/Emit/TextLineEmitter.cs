using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Emit.GeneratorFontResolver;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class TextLineEmitter(
    FontCollection fonts,
    GeneratorFontResolver fontResolver,
    ImageStore imageStore,
    StyleResolution resolution,
    StructureTreeBuilder structureTree)
{
    private readonly List<byte> scratchBytes = [];
    private readonly SfntRunBuilder runBuilder = new(fonts, fontResolver);

    public Dictionary<string, GeneratedAnchor> Anchors { get; } = new(StringComparer.Ordinal);

    public void EmitBandLines(
        EmitContext context,
        IReadOnlyList<PositionedLine> lines,
        double left,
        double top,
        double width)
        => EmitFieldExpandedLines(
            context, lines,
            static l => l.Line, static l => l.Source, static _ => 0, static l => l.Y,
            left, top, delta: 0, width,
            opacity: 1, inherited: null, resolveStructure: false,
            overflowThreshold: double.PositiveInfinity);

    public bool EmitFieldExpandedLines<TLine>(
        EmitContext context,
        IReadOnlyList<TLine> lines,
        Func<TLine, LineBox> lineOf,
        Func<TLine, Block> sourceOf,
        Func<TLine, double> xOf,
        Func<TLine, double> yOf,
        double left,
        double baseTop,
        double delta,
        double width,
        double opacity,
        StructureElement? inherited,
        bool resolveStructure,
        double overflowThreshold)
    {
        var overflows = false;
        var i = 0;
        while (i < lines.Count)
        {
            var current = lines[i];
            var source = sourceOf(current);
            var originX = left + xOf(current);
            var element = resolveStructure ? structureTree.ElementOf(source) ?? inherited : null;
            var marker = resolveStructure ? structureTree.MarkerElementOf(source) : null;
            if (source is Paragraph paragraph && context.Fields.HasField(paragraph))
            {
                var reserved = 0;
                while (i + reserved < lines.Count && sourceOf(lines[i + reserved]) == source)
                {
                    reserved++;
                }

                var y = yOf(current);
                foreach (var box in context.Fields.ResolveFields(paragraph, width, context.PageNumber, context.PageCount, resolution.Alignment(paragraph), reserved))
                {
                    EmitLine(context, box, originX, baseTop - (y + delta), element, opacity, marker);
                    overflows |= box.Width > overflowThreshold + 0.01;
                    y += box.Height;
                }

                i += reserved;
            }
            else
            {
                var box = lineOf(current);
                EmitLine(context, box, originX, baseTop - (yOf(current) + delta), element, opacity, marker);
                overflows |= box.Width > overflowThreshold + 0.01;
                i++;
            }
        }

        return overflows;
    }

    public void EmitLine(EmitContext context, LineBox line, double originX, double baseline, StructureElement? element, double opacity = 1, StructureElement? markerElement = null)
    {
        var plan = context.Plan;
        var y = baseline - line.Baseline;
        foreach (var fragment in line.Fragments)
        {
            if (fragment.Run.Anchor is { Length: > 0 } anchor)
            {
                Anchors.TryAdd(anchor, new GeneratedAnchor(context.PageNumber - 1, baseline));
            }
        }

        var lineFragments = CoalesceFragments(line.Fragments);
        for (var fi = 0; fi < lineFragments.Count; fi++)
        {
            var fragment = lineFragments[fi];
            var alpha = opacity * fragment.Run.Opacity;
            var fragElement = fragment.IsMarker && markerElement is not null ? markerElement : element;
            if (fragment.Run is InlineImage inlineImage)
            {
                EmitInlineImage(plan, inlineImage, originX + fragment.XOffset, y, fragElement, alpha);
                continue;
            }

            var text = fragment.Text;
            if (text.Length == 0)
            {
                continue;
            }

            var extGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null;
            var font = fragment.Font;
            if (fonts.TryResolvePrimary(font, out _))
            {
                EmitSfntFragment(plan, fragment, originX + fragment.XOffset, y, fragElement, extGState);
            }
            else
            {
                EmitBase14Fragment(plan, fragment, font, originX + fragment.XOffset, y, fragElement, extGState);
            }
        }

        EmitDecorations(plan, line, originX, y, opacity, underline: true);
        EmitDecorations(plan, line, originX, y, opacity, underline: false);
        EmitLinks(plan, line, originX, y);
    }

    private static IEnumerable<(int First, double Start, double End)> Spans(
        IReadOnlyList<LineFragment> fragments,
        Func<LineFragment, bool> eligible,
        Func<LineFragment, LineFragment, bool> sameGroup)
    {
        var i = 0;
        while (i < fragments.Count)
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
            while (j < fragments.Count && sameGroup(first, fragments[j]))
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            yield return (i, start, end);
            i = j;
        }
    }

    private static double DecorationThickness(double size) => Math.Max(size * 0.06, 0.5);

    private static void AddDecorationEdge(PagePlan plan, double x1, double x2, double edgeY, Font font, double alpha)
    {
        plan.Edges.Add(new EdgeDraw
        {
            X1 = x1,
            Y1 = edgeY,
            X2 = x2,
            Y2 = edgeY,
            LineWidth = DecorationThickness(font.Size),
            Color = font.Color,
            Style = BorderStyle.Solid,
            ExtGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null,
        });
    }

    private static string? LinkUri(Run run) => run.Link is { Length: > 0 } link ? link : null;

    private static string? LinkDestination(Run run) =>
        LinkUri(run) is null && run.LinkToAnchor is { Length: > 0 } anchor ? anchor : null;

    private static void EmitLinks(PagePlan plan, LineBox line, double originX, double y)
    {
        foreach (var (first, start, end) in Spans(
            line.Fragments,
            f => (LinkUri(f.Run) is not null || LinkDestination(f.Run) is not null) && f.Text.Length > 0,
            (f, next) => next.Run == f.Run))
        {
            var fragment = line.Fragments[first];
            var size = fragment.Font.Size;
            plan.Links.Add(new GeneratedLink
            {
                X1 = originX + start,
                Y1 = y - (size * 0.3),
                X2 = originX + end,
                Y2 = y + (size * 0.9),
                Uri = LinkUri(fragment.Run),
                Destination = LinkDestination(fragment.Run),
            });
        }
    }

    private static void EmitDecorations(
        PagePlan plan, LineBox line, double originX, double y, double opacity, bool underline)
    {
        foreach (var (first, start, end) in Spans(
            line.Fragments,
            f => (underline ? f.Font.Underline : f.Font.Strikethrough) && f.Text.Length > 0,
            (f, next) => underline
                ? next.Run == f.Run
                : next.Font.Strikethrough
                    && next.Font.Size == f.Font.Size
                    && next.Font.Color.Equals(f.Font.Color)
                    && next.Run.Opacity == f.Run.Opacity))
        {
            var fragment = line.Fragments[first];
            var font = fragment.Font;
            AddDecorationEdge(
                plan,
                originX + start,
                originX + end,
                y + font.Size * (underline ? -0.12 : 0.3),
                font,
                opacity * fragment.Run.Opacity);
        }
    }

    private List<LineFragment> CoalesceFragments(IReadOnlyList<LineFragment> fragments)
    {
        var result = new List<LineFragment>(fragments.Count);
        var i = 0;
        while (i < fragments.Count)
        {
            var current = fragments[i];
            var run = current.Run;
            var text = run.Text;
            var end = current.Start + current.Length;
            var right = current.XOffset + current.Advance;
            var j = i + 1;
            var mergeable = run.LetterSpacing.Point == 0 && run.VerticalAlign == RunVerticalAlign.None;
            while (j < fragments.Count && current.Length > 0 && mergeable)
            {
                var next = fragments[j];
                if (next.Run != run || next.Length == 0 || next.Start < end || next.Start > text.Length)
                {
                    break;
                }

                var allSpaces = true;
                for (var g = end; g < next.Start; g++)
                {
                    if (text[g] != ' ')
                    {
                        allSpaces = false;
                        break;
                    }
                }

                var gapWidth = next.Start == end
                    ? 0
                    : RunTextAdvance.Measure(
                        fonts, run, current.Font, text[end..next.Start],
                        leadingCharacterSpacing: true, trailingCharacterSpacing: true);
                if (!allSpaces || Math.Abs(next.XOffset - right - gapWidth) > 0.001)
                {
                    break;
                }

                end = next.Start + next.Length;
                right = next.XOffset + next.Advance;
                j++;
            }

            if (j > i + 1)
            {
                result.Add(new LineFragment
                {
                    Run = run,
                    Font = current.Font,
                    Text = text[current.Start..end],
                    Start = current.Start,
                    Length = end - current.Start,
                    XOffset = current.XOffset,
                    Advance = right - current.XOffset,
                });
            }
            else
            {
                result.Add(current);
            }

            i = j;
        }

        return result;
    }

    private static TextDraw BuildTextDraw(
        Run run,
        Font font,
        double x,
        double baseline,
        double size,
        GeneratedFont generated,
        byte[] bytes,
        StructureElement? element,
        string? extGState,
        double[]? kerns,
        double strokeWidth = 0,
        double shear = 0)
        => new()
        {
            X = x,
            Baseline = baseline,
            Size = size,
            Color = font.Color,
            Font = generated,
            Bytes = bytes,
            StrokeWidth = strokeWidth,
            Shear = shear,
            CharSpacing = run.LetterSpacing.Point,
            Rise = run.ScriptRise(font.Size),
            WordSpacing = run.WordSpacing.Point,
            HorizontalScale = run.HorizontalScale,
            RenderMode = run.Invisible ? 3 : 0,
            FillPaint = run.FillPaint,
            ExtGState = extGState,
            Element = element,
            Kerns = kerns,
        };

    private void EmitBase14Fragment(PagePlan plan, LineFragment fragment, Font font, double startX, double y, StructureElement? element, string? extGState)
    {
        var metrics = Base14Metrics.Resolve(font) ?? Base14Metrics.Resolve(new Font())!;
        var run = fragment.Run;
        var size = font.Size * run.ScriptScale;
        var text = fragment.Text;
        var x = startX;

        var i = 0;
        while (i < text.Length)
        {
            if (fonts.TryResolveFallbackGlyph(CodePointAt(text, i), out var face, out _) && !IsWinAnsi(CodePointAt(text, i)))
            {
                var generated = fontResolver.ResolveSfnt(face);
                var glyphRun = new SfntRunAccumulator(face, generated, font.Size, fonts.EnableKerning, scratchBytes);
                glyphRun.Begin();
                while (i < text.Length)
                {
                    var codepoint = CodePointAt(text, i);
                    if (IsWinAnsi(codepoint)
                        || !fonts.TryResolveFallbackGlyph(codepoint, out var candidate, out var gid)
                        || candidate != face)
                    {
                        break;
                    }

                    glyphRun.Append(gid, codepoint);
                    i += codepoint > 0xFFFF ? 2 : 1;
                }

                plan.UsedFonts.Add(generated);
                plan.Texts.Add(BuildTextDraw(run, font, x, y, size, generated, glyphRun.Bytes, element, extGState, glyphRun.Kerns));

                x += RunTextAdvance.Calculate(
                    glyphRun.Advance, glyphRun.GlyphCount, glyphRun.WordSpaceCount, run,
                    trailingCharacterSpacing: true);
            }
            else
            {
                var builderText = new StringBuilder();
                while (i < text.Length)
                {
                    var codepoint = CodePointAt(text, i);
                    if (IsWinAnsi(codepoint))
                    {
                        builderText.Append((char)codepoint);
                    }
                    else if (!fonts.TryResolveFallbackGlyph(codepoint, out _, out _))
                    {
                        builderText.Append('?');
                    }
                    else
                    {
                        break;
                    }

                    i += codepoint > 0xFFFF ? 2 : 1;
                }

                var segment = builderText.ToString();
                var generated = fontResolver.ResolveBase14(font);
                plan.UsedFonts.Add(generated);

                double[]? kerns = null;
                var kernPoints = 0.0;
                if (fonts.EnableKerning && segment.Length > 1)
                {
                    var list = new List<double>(segment.Length - 1);
                    for (var k = 1; k < segment.Length; k++)
                    {
                        var kern = metrics.GetRunKerning(segment[k - 1], segment[k]);
                        kernPoints += kern * font.Size / 1000.0;
                        list.Add(-kern);
                    }

                    kerns = SfntRunBuilder.HasNonZero(list) ? [.. list] : null;
                }

                plan.Texts.Add(BuildTextDraw(
                    run, font, x, y, size, generated,
                    WinAnsiText.Encode(segment, OnUnencodable.Substitute), element, extGState, kerns));

                x += RunTextAdvance.Calculate(
                    metrics.MeasureString(segment, font.Size) + kernPoints,
                    segment.Length,
                    CountSpaces(segment),
                    run,
                    trailingCharacterSpacing: true);
            }
        }
    }

    private void EmitSfntFragment(PagePlan plan, LineFragment fragment, double startX, double y, StructureElement? element, string? extGState)
    {
        var run = fragment.Run;
        var font = fragment.Font;
        var size = font.Size * run.ScriptScale;
        var runX = startX;

        foreach (var glyphRun in runBuilder.Build(fragment.Text, font, font.Size))
        {
            var face = glyphRun.Face;
            plan.UsedFonts.Add(glyphRun.Font);
            plan.Texts.Add(BuildTextDraw(
                run, font, runX, y, size, glyphRun.Font, glyphRun.Bytes, element, extGState, glyphRun.Kerns,
                font.Bold && !face.Bold ? size * 0.03 : 0,
                font.Italic && !face.Italic ? 0.21 : 0));

            runX += RunTextAdvance.Calculate(
                glyphRun.Advance, glyphRun.GlyphCount, glyphRun.WordSpaceCount, run,
                trailingCharacterSpacing: true);
        }
    }

    private static int CountSpaces(string text)
    {
        var count = 0;
        foreach (var character in text)
        {
            if (character == ' ')
            {
                count++;
            }
        }

        return count;
    }

    private void EmitInlineImage(PagePlan plan, InlineImage image, double x, double baseline, StructureElement? element, double alpha)
    {
        var (width, height) = image.EffectiveSize();
        var generated = imageStore.DecodeBytes(image, image.Data);
        plan.Images.Add(new ImageDraw
        {
            X = x,
            Y = baseline,
            Width = width,
            Height = height,
            Image = generated,
            Element = element,
            ExtGState = alpha < 1 ? plan.RegisterExtGState(alpha, alpha) : null,
        });
        plan.UsedImages.Add(generated);
    }
}
