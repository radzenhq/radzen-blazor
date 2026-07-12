using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using static Radzen.Documents.Pdf.GeneratorFontResolver;

namespace Radzen.Documents.Pdf;

// Emits a laid-out line: coalesces fragments, draws each text run through its owning
// embedded subset (sfnt) or the base-14 WinAnsi path, places inline images on the
// baseline, and strokes the line's underlines, strikethroughs and link rects.
internal sealed class TextLineEmitter(
    FontCollection fonts,
    GeneratorFontResolver fontResolver,
    ImageStore imageStore,
    StyleResolution resolution)
{
    private readonly List<byte> scratchBytes = [];

    // Named destinations recorded at emit time: anchor name -> (page, line top).
    // First occurrence wins so a run split across pages anchors where it starts.
    public Dictionary<string, GeneratedAnchor> Anchors { get; } = new(System.StringComparer.Ordinal);

    // Header/footer bands are laid out once per section and reused on every page, so
    // a paragraph containing page-number fields is re-resolved here at emit time with
    // the actual page number and total count substituted.
    public void EmitBandLines(
        EmitContext context,
        IReadOnlyList<PositionedLine> lines,
        double left,
        double top,
        double width)
    {
        var pageNumber = context.PageNumber;
        var pageCount = context.PageCount;
        var i = 0;
        while (i < lines.Count)
        {
            var line = lines[i];
            if (line.Source is Paragraph paragraph && context.Fields.HasField(paragraph))
            {
                var y = line.Y;
                foreach (var box in context.Fields.ResolveFields(paragraph, width, pageNumber, pageCount, resolution.Alignment(paragraph)))
                {
                    EmitLine(context, box, left, top - y, null);
                    y += box.Height;
                }

                while (i < lines.Count && lines[i].Source == paragraph)
                {
                    i++;
                }
            }
            else
            {
                EmitLine(context, line.Line, left, top - line.Y, null);
                i++;
            }
        }
    }

    public void EmitLine(EmitContext context, LineBox line, double originX, double baseline, StructureElement? element)
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
            if (fragment.Run is InlineImage inlineImage)
            {
                EmitInlineImage(plan, inlineImage, originX + fragment.XOffset, y, element);
                continue;
            }

            var text = fragment.Text;
            if (text.Length == 0)
            {
                continue;
            }

            var font = fragment.Run.ResolvedFont;
            if (fonts.TryResolvePrimary(font, out var primary))
            {
                EmitSfntFragment(plan, fragment, primary, originX + fragment.XOffset, y, element);
            }
            else
            {
                EmitBase14Fragment(plan, fragment, font, originX + fragment.XOffset, y, element);
            }
        }

        EmitUnderlines(plan, line, originX, y);
        EmitStrikethroughs(plan, line, originX, y);
        EmitLinks(plan, line, originX, y);
    }

    // One /Link rect per maximal group of consecutive fragments of the same linked
    // run on this line; a run wrapped over several lines gets one rect per line.
    private static void EmitLinks(PagePlan plan, LineBox line, double originX, double y)
    {
        var fragments = line.Fragments;
        var i = 0;
        while (i < fragments.Count)
        {
            var run = fragments[i].Run;
            var uri = run.Link is { Length: > 0 } link ? link : null;
            var destination = uri is null && run.LinkToAnchor is { Length: > 0 } anchor ? anchor : null;
            if ((uri is null && destination is null) || fragments[i].Text.Length == 0)
            {
                i++;
                continue;
            }

            var start = fragments[i].XOffset;
            var end = start + fragments[i].Advance;
            var j = i + 1;
            while (j < fragments.Count && fragments[j].Run == run)
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            var size = run.ResolvedFont.Size;
            plan.Links.Add(new GeneratedLink
            {
                X1 = originX + start,
                Y1 = y - (size * 0.3),
                X2 = originX + end,
                Y2 = y + (size * 0.9),
                Uri = uri,
                Destination = destination,
            });

            i = j;
        }
    }

    // One underline per maximal group of consecutive fragments of the same underlined
    // run, spanning from the first fragment's start to the last fragment's end so
    // inter-word gaps inside the run stay underlined.
    private static void EmitUnderlines(PagePlan plan, LineBox line, double originX, double y)
    {
        var fragments = line.Fragments;
        var i = 0;
        while (i < fragments.Count)
        {
            var run = fragments[i].Run;
            var font = run.ResolvedFont;
            if (!font.Underline || fragments[i].Text.Length == 0)
            {
                i++;
                continue;
            }

            var start = fragments[i].XOffset;
            var end = fragments[i].XOffset + fragments[i].Advance;
            var j = i + 1;
            while (j < fragments.Count && fragments[j].Run == run)
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            var underlineY = y - (font.Size * 0.12);
            plan.Edges.Add(new EdgeDraw
            {
                X1 = originX + start,
                Y1 = underlineY,
                X2 = originX + end,
                Y2 = underlineY,
                LineWidth = System.Math.Max(font.Size * 0.06, 0.5),
                Color = font.Color,
                Style = BorderStyle.Solid,
            });

            i = j;
        }
    }

    // One strike per maximal group of consecutive strikethrough fragments that share
    // size and color (across runs), drawn at roughly the x-height midline above the
    // baseline; a change in size or color starts a new correctly-styled line.
    private static void EmitStrikethroughs(PagePlan plan, LineBox line, double originX, double y)
    {
        var fragments = line.Fragments;
        var i = 0;
        while (i < fragments.Count)
        {
            var font = fragments[i].Run.ResolvedFont;
            if (!font.Strikethrough || fragments[i].Text.Length == 0)
            {
                i++;
                continue;
            }

            var start = fragments[i].XOffset;
            var end = fragments[i].XOffset + fragments[i].Advance;
            var j = i + 1;
            while (j < fragments.Count
                && fragments[j].Run.ResolvedFont.Strikethrough
                && fragments[j].Run.ResolvedFont.Size == font.Size
                && fragments[j].Run.ResolvedFont.Color.Equals(font.Color))
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            var strikeY = y + (font.Size * 0.3);
            plan.Edges.Add(new EdgeDraw
            {
                X1 = originX + start,
                Y1 = strikeY,
                X2 = originX + end,
                Y2 = strikeY,
                LineWidth = System.Math.Max(font.Size * 0.06, 0.5),
                Color = font.Color,
                Style = BorderStyle.Solid,
            });

            i = j;
        }
    }

    // Consecutive fragments of the same run whose positional gap equals the source
    // whitespace between them collapse into one fragment with the spaces intact, so
    // a plain line is drawn as one text run. Tabs and justified gaps never match the
    // measured space width and keep their fragments separate.
    private List<LineFragment> CoalesceFragments(IReadOnlyList<LineFragment> fragments)
    {
        var result = new List<LineFragment>(fragments.Count);
        var spaceWidths = new Dictionary<Font, double>();
        var i = 0;
        while (i < fragments.Count)
        {
            var current = fragments[i];
            var run = current.Run;
            var text = run.Text;
            var end = current.Start + current.Length;
            var right = current.XOffset + current.Advance;
            var j = i + 1;
            while (j < fragments.Count && current.Length > 0)
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

                var gapWidth = next.Start == end ? 0 : (next.Start - end) * SpaceWidth(run.ResolvedFont, spaceWidths);
                if (!allSpaces || System.Math.Abs(next.XOffset - right - gapWidth) > 0.001)
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

    // Spaces carry no kerning, so a gap of N spaces measures as N * one space width; cache the
    // per-font single-space advance rather than measuring a fresh substring per gap.
    private double SpaceWidth(Font font, Dictionary<Font, double> cache)
    {
        if (!cache.TryGetValue(font, out var width))
        {
            width = fonts.MeasureText(" ", font);
            cache[font] = width;
        }

        return width;
    }

    // Base-14 WinAnsi path. Characters outside cp1252 are never dropped: they render
    // through the registered fallback chain when it supplies a glyph, otherwise a
    // visible '?' placeholder is substituted.
    private void EmitBase14Fragment(PagePlan plan, LineFragment fragment, Font font, double startX, double y, StructureElement? element)
    {
        var metrics = Base14Metrics.Resolve(font) ?? Base14Metrics.Resolve(new Font())!;
        var size = font.Size;
        var text = fragment.Text;
        var x = startX;

        var i = 0;
        while (i < text.Length)
        {
            if (fonts.TryResolveFallbackGlyph(CodePointAt(text, i), out var face, out _) && !IsWinAnsi(CodePointAt(text, i)))
            {
                var generated = fontResolver.ResolveSfnt(face);
                var bytes = scratchBytes;
                bytes.Clear();
                var advance = 0.0;
                while (i < text.Length)
                {
                    var codepoint = CodePointAt(text, i);
                    if (IsWinAnsi(codepoint)
                        || !fonts.TryResolveFallbackGlyph(codepoint, out var candidate, out var gid)
                        || candidate != face)
                    {
                        break;
                    }

                    // First-seen codepoint wins so glyphs shared by several codepoints
                    // (e.g. hyphen/soft-hyphen) map deterministically, not by draw order.
                    generated.GidToUnicode.TryAdd(gid, codepoint);
                    bytes.Add((byte)(gid >> 8));
                    bytes.Add((byte)(gid & 0xFF));
                    advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
                    i += codepoint > 0xFFFF ? 2 : 1;
                }

                plan.UsedFonts.Add(generated);
                plan.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = y,
                    Size = size,
                    Color = font.Color,
                    Font = generated,
                    Bytes = [.. bytes],
                    Element = element,
                });

                x += advance;
            }
            else
            {
                var builderText = new System.Text.StringBuilder();
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
                plan.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = y,
                    Size = size,
                    Color = font.Color,
                    Font = generated,
                    Bytes = EncodeWinAnsi(segment),
                    Element = element,
                });

                x += metrics.MeasureString(segment, size);
            }
        }
    }

    // Splits a fragment into maximal sub-runs by the physical face that actually
    // supplies each glyph (primary or a SetFallback face), so every glyph is drawn
    // by the embedded subset that owns it - not the primary's .notdef.
    private void EmitSfntFragment(PagePlan plan, LineFragment fragment, SfntFont primary, double startX, double y, StructureElement? element)
    {
        var font = fragment.Run.ResolvedFont;
        var size = font.Size;
        var text = fragment.Text;
        var runX = startX;

        var i = 0;
        while (i < text.Length)
        {
            var (face, _) = fonts.ResolveGlyph(primary, CodePointAt(text, i));
            var generated = fontResolver.ResolveSfnt(face);
            var bytes = scratchBytes;
            bytes.Clear();
            var advance = 0.0;
            while (i < text.Length)
            {
                var codepoint = CodePointAt(text, i);
                var (candidate, gid) = fonts.ResolveGlyph(primary, codepoint);
                if (candidate != face)
                {
                    break;
                }

                // First-seen codepoint wins so glyphs shared by several codepoints
                // (e.g. hyphen/soft-hyphen) map deterministically, not by draw order.
                generated.GidToUnicode.TryAdd(gid, codepoint);
                bytes.Add((byte)(gid >> 8));
                bytes.Add((byte)(gid & 0xFF));
                advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
                i += codepoint > 0xFFFF ? 2 : 1;
            }

            plan.UsedFonts.Add(generated);
            plan.Texts.Add(new TextDraw
            {
                X = runX,
                Baseline = y,
                Size = size,
                Color = font.Color,
                Font = generated,
                Bytes = [.. bytes],
                Element = element,
                // Synthetic bold: no real bold face is available, so the glyphs are
                // thickened by fill+stroke with a small stroke width at emission.
                StrokeWidth = font.Bold && !face.Bold ? size * 0.03 : 0,
                // Synthetic italic: no real italic face, so the run is slanted by a
                // sheared text matrix (tan of about 12 degrees).
                Shear = font.Italic && !face.Italic ? 0.21 : 0,
            });

            runX += advance;
        }
    }

    // Draws an inline image sitting on the line baseline: its bottom edge is at the baseline y,
    // so it advances the line by its width and shares the line height computed by the breaker.
    private void EmitInlineImage(PagePlan plan, InlineImage image, double x, double baseline, StructureElement? element)
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
        });
        plan.UsedImages.Add(generated);
    }
}
