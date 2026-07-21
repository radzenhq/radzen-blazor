using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf;

/// <summary>Specifies the optional appearance painted after content is removed.</summary>
public sealed class RedactionOptions
{
    /// <summary>Gets or sets the fill color, or <c>null</c> to paint no overlay.</summary>
    public Color? FillColor { get; set; }
}

internal static class Redactor
{
    public static int RedactText(Page page, string text, TextSearchOptions? searchOptions, RedactionOptions? redactionOptions)
    {
        var cache = new ContentTokenizer.Cache();
        var hits = page.FindText(text, searchOptions, -1, cache);
        if (hits.Count == 0)
        {
            return 0;
        }

        if (hits.Any(static hit => hit.GeometryEstimated))
        {
            throw new NotSupportedException("The source font does not provide a usable width for every matched glyph, so the bounds of the match are an estimate that cannot be redacted safely.");
        }

        Redact(page, hits.Select(static hit => hit.Bounds), redactionOptions, cache);
        return hits.Count;
    }

    public static void Redact(Page page, IEnumerable<PdfRect> areas, RedactionOptions? options, ContentTokenizer.Cache? cache = null)
    {
        ArgumentNullException.ThrowIfNull(areas);
        cache ??= new ContentTokenizer.Cache();
        var regions = areas.ToArray();
        foreach (var area in regions)
        {
            if (!area.IsFiniteAndPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(areas), "Redaction regions must have finite coordinates and positive dimensions.");
            }
        }

        if (regions.Length == 0)
        {
            return;
        }

        page.BeginGeneratedEdit();

        Func<string, bool> isRemovableXObject = static _ => false;
        if (page.Generated is { } generated)
        {
            var imageNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var image in generated.Images)
            {
                imageNames.Add(image.Key);
            }

            isRemovableXObject = imageNames.Contains;
        }

        if (page.CurrentContent is { Length: > 0 } raw)
        {
            var selected = SelectIntersectingGlyphs(page, regions, cache);
            if (selected.Count > 0)
            {
                page.ApplyEditedContent(RemoveTextGlyphs(raw, selected, cache));
            }
        }

        SweepElements(page.Content, regions, isRemovableXObject);

        if (options?.FillColor is { } fill)
        {
            var content = page.Content;
            foreach (var area in regions)
            {
                var overlay = PathContent.Rectangle(area.Left, area.Bottom, area.Width, area.Height);
                overlay.Fill = true;
                overlay.FillColor = fill;
                content.Add(overlay);
            }
        }
    }

    private static Dictionary<int, (PositionedTextRun Run, bool[] Removed)> SelectIntersectingGlyphs(
        Page page, IReadOnlyList<PdfRect> regions, ContentTokenizer.Cache cache)
    {
        var selected = new Dictionary<int, (PositionedTextRun Run, bool[] Removed)>();
        foreach (var run in page.ExtractPositionedText(cache))
        {
            if (run.GeometryEstimated)
            {
                if (MayReach(run, regions))
                {
                    throw new NotSupportedException("The source font does not provide a usable width for every glyph shown by a text operator near a redaction region, so which of its glyphs the region covers cannot be determined safely.");
                }

                continue;
            }

            var removed = new bool[run.Text.Length];
            var any = false;
            for (var i = 0; i < removed.Length; i++)
            {
                removed[i] = IntersectsAny(run.CharacterQuadrilateral(i).Bounds, regions);
                any |= removed[i];
            }

            if (any)
            {
                selected.Add(run.OperatorIndex, (run, removed));
            }
        }

        return selected;
    }

    private static void SweepElements(ContentCollection content, IReadOnlyList<PdfRect> regions, Func<string, bool> isRemovableXObject)
    {
        for (var i = content.Count - 1; i >= 0; i--)
        {
            switch (content[i])
            {
                case PathContent path when path.GetBounds() is { } bounds && IntersectsAny(bounds, regions):
                    if (path.Clip != PathClipMode.None)
                    {
                        throw new NotSupportedException("A redaction region intersects a clipping path that cannot be removed safely.");
                    }

                    content.RemoveAt(i);
                    break;
                case ImageContent image when IntersectsAny(TransformedBounds(image.Bounds, image.Transform), regions):
                    content.RemoveAt(i);
                    break;
                case XObjectContent xobject when IntersectsAny(UnitBounds(xobject.Transform), regions):
                    if (!isRemovableXObject(xobject.Name))
                    {
                        throw new NotSupportedException($"A redaction region intersects XObject '{xobject.Name}'. Its image or form subtype cannot be determined safely from the content stream.");
                    }

                    content.RemoveAt(i);
                    break;
                case InlineImageContent inline when IntersectsAny(UnitBounds(inline.Transform), regions):
                    content.RemoveAt(i);
                    break;
                case RawContent unmodeled when ContentOperatorClass.MayPaintUnknown(unmodeled.Operator)
                    && (unmodeled.ClipBounds is not { } clip || IntersectsAny(clip, regions)):
                    throw new NotSupportedException($"A redaction region intersects content painted by the '{unmodeled.Operator}' operator. Its extent cannot be determined safely from the content stream.");
                case RawContent:
                    break;
            }
        }
    }

    private static byte[] RemoveTextGlyphs(byte[] source, IReadOnlyDictionary<int, (PositionedTextRun Run, bool[] Removed)> selected, ContentTokenizer.Cache? cache)
    {
        var edits = new List<ContentEdit>();
        ContentTextWalker.Walk(source, null, (walker, op, operands, array, operatorIndex) =>
        {
            if (selected.TryGetValue(operatorIndex, out var selection))
            {
                if (op is "'" or "\"")
                {
                    throw new NotSupportedException($"Redacting text shown by the '{op}' operator cannot preserve line positioning safely.");
                }

                var start = op == "TJ" ? walker.ArrayStart : walker.OperandStart;
                if (start < 0)
                {
                    throw new FormatException("The text-show operator has no valid operand.");
                }

                edits.Add(new ContentEdit(start, walker.Operator.End, BuildRedactedShow(selection.Run, selection.Removed)));
            }

            return 0.0;
        }, cache);

        return ContentEdits.Apply(source, edits);
    }

    private static byte[] BuildRedactedShow(PositionedTextRun run, IReadOnlyList<bool> removed)
    {
        var denominator = ContentWriter.RequireTjScale(run.FontSize, run.Scale, "Redacting");

        using var writer = new ContentWriter();
        writer.WriteRaw("[");
        WriteAdvance(writer, run.AdvanceOffsets[0], denominator);
        for (var i = 0; i < run.Text.Length; i++)
        {
            var desiredAdvance = run.AdvanceOffsets[i + 1] - run.AdvanceOffsets[i];
            if (removed[i])
            {
                WriteAdvance(writer, desiredAdvance, denominator);
                continue;
            }

            var character = run.Text.Substring(i, 1);
            if (!run.Font.TryEncode(character, out var bytes) || run.Font.DecodeCodes(bytes).Count != 1)
            {
                throw new NotSupportedException("A redacted show operator contains a glyph cluster that cannot be split safely.");
            }

            var code = run.Font.DecodeCodes(bytes)[0];
            writer.WriteString(bytes);
            var nominalAdvance = LoadedGlyphAdvance.Calculate(
                run.Font, code.Code, code.IsWordSpace, run.FontSize, run.Scale,
                run.CharSpacing, run.WordSpacing, MissingWidthPolicy.Throw, out _);
            WriteAdvance(writer, desiredAdvance - nominalAdvance, denominator);
        }

        writer.WriteRaw("] TJ");
        return writer.ToArray();
    }

    private static void WriteAdvance(ContentWriter writer, double advance, double denominator)
        => writer.WriteTjAdjustment(-advance, denominator);

    private static PdfRect UnitBounds(Matrix transform) => TransformedBounds(new PdfRect(0, 0, 1, 1), transform);

    private static PdfRect TransformedBounds(PdfRect rect, Matrix transform)
    {
        var bounds = new PdfRectBounds();
        foreach (var point in new[]
        {
            transform.Transform(rect.Left, rect.Bottom),
            transform.Transform(rect.Right, rect.Bottom),
            transform.Transform(rect.Right, rect.Top),
            transform.Transform(rect.Left, rect.Top),
        })
        {
            bounds.Include(point.X, point.Y);
        }

        return bounds.ToRect();
    }

    private static bool MayReach(PositionedTextRun run, IReadOnlyList<PdfRect> regions)
    {
        if (!run.Matrix.TryInvert(out var inverse))
        {
            return true;
        }

        var top = Math.Max(run.FontSize, 0);
        var bottom = Math.Min(run.FontSize, 0);
        foreach (var area in regions)
        {
            var corners = new[]
            {
                inverse.Transform(area.Left, area.Bottom), inverse.Transform(area.Right, area.Bottom),
                inverse.Transform(area.Right, area.Top), inverse.Transform(area.Left, area.Top),
            };

            if (!corners.All(p => p.X < 0) && !corners.All(p => p.Y < bottom) && !corners.All(p => p.Y > top))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IntersectsAny(PdfRect bounds, IReadOnlyList<PdfRect> regions)
    {
        foreach (var area in regions)
        {
            if (bounds.Left < area.Right && bounds.Right > area.Left && bounds.Bottom < area.Top && bounds.Top > area.Bottom)
            {
                return true;
            }
        }

        return false;
    }
}
