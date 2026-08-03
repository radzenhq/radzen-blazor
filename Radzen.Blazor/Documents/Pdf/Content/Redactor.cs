using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

/// <summary>Specifies the optional appearance painted after content is removed.</summary>
public sealed class RedactionOptions
{
    /// <summary>Gets or sets the fill color, or <c>null</c> to paint no overlay.</summary>
    public Color? FillColor { get; set; }
}

internal sealed class RedactionPlan
{
    private readonly Page page;
    private readonly bool beginGeneratedEdit;
    private readonly byte[]? editedContent;
    private readonly int elementCount;
    private readonly int[] removals;
    private readonly PathContent[] overlays;

    internal RedactionPlan(Page page, bool beginGeneratedEdit, byte[]? editedContent, int elementCount, int[] removals, PathContent[] overlays)
    {
        this.page = page;
        this.beginGeneratedEdit = beginGeneratedEdit;
        this.editedContent = editedContent;
        this.elementCount = elementCount;
        this.removals = removals;
        this.overlays = overlays;
    }

    internal void Commit()
    {
        if (beginGeneratedEdit)
        {
            page.BeginGeneratedEdit();
        }

        if (editedContent is not null)
        {
            page.ApplyEditedContent(editedContent);
        }

        var content = page.Content;
        if (content.Count != elementCount)
        {
            throw new InvalidOperationException("The page content changed while the redaction was being planned.");
        }

        foreach (var index in removals)
        {
            content.RemoveAt(index);
        }

        foreach (var overlay in overlays)
        {
            content.Add(overlay);
        }
    }
}

internal static class Redactor
{
    public static int RedactText(Page page, string text, TextSearchOptions? searchOptions, RedactionOptions? redactionOptions)
    {
        var plan = PlanText(page, text, searchOptions, redactionOptions, out var count);
        plan?.Commit();
        return count;
    }

    public static RedactionPlan? PlanText(Page page, string text, TextSearchOptions? searchOptions, RedactionOptions? redactionOptions, out int count)
    {
        var cache = new ContentTokenizer.Cache();
        var hits = page.FindText(text, searchOptions, -1, cache);
        count = hits.Count;
        if (hits.Count == 0)
        {
            return null;
        }

        if (hits.Any(static hit => hit.GeometryEstimated))
        {
            throw new NotSupportedException("The source font does not provide a usable width for every matched glyph, so the bounds of the match are an estimate that cannot be redacted safely.");
        }

        return Plan(page, hits.Select(static hit => hit.Bounds), redactionOptions, cache);
    }

    public static void Redact(Page page, IEnumerable<PdfRect> areas, RedactionOptions? options, ContentTokenizer.Cache? cache = null)
        => Plan(page, areas, options, cache)?.Commit();

    public static RedactionPlan? Plan(Page page, IEnumerable<PdfRect> areas, RedactionOptions? options, ContentTokenizer.Cache? cache = null)
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
            return null;
        }

        var beginGeneratedEdit = page.IsGenerated && !page.IsEditingGenerated;
        var raw = beginGeneratedEdit ? page.RawContent : page.CurrentContent;

        byte[]? edited = null;
        if (raw is { Length: > 0 })
        {
            var selected = SelectIntersectingGlyphs(raw, page.TextFonts, regions, cache);
            if (selected.Count > 0)
            {
                edited = RemoveTextGlyphs(raw, selected, cache);
            }
        }

        var swept = edited ?? raw;
        IReadOnlyList<ContentElement> elements;
        if (swept is { Length: > 0 })
        {
            var parsed = new ContentCollection();
            ContentInterpreter.Materialize(swept, parsed, page.TextFonts, cache);
            elements = parsed;
        }
        else
        {
            elements = page.Content;
        }

        var removals = PlanRemovals(elements, regions, RemovableXObjects(page));
        var overlays = Array.Empty<PathContent>();
        if (options?.FillColor is { } fill)
        {
            overlays = new PathContent[regions.Length];
            for (var i = 0; i < regions.Length; i++)
            {
                var area = regions[i];
                var overlay = PathContent.Rectangle(area.Left, area.Bottom, area.Width, area.Height);
                overlay.Fill = true;
                overlay.FillColor = fill;
                overlays[i] = overlay;
            }
        }

        return new RedactionPlan(page, beginGeneratedEdit, edited, elements.Count, removals, overlays);
    }

    private static Func<string, bool> RemovableXObjects(Page page)
    {
        if (page.OutputIdentity is not { } generated)
        {
            return static _ => false;
        }

        var imageNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var image in generated.Images)
        {
            imageNames.Add(image.Key);
        }

        return imageNames.Contains;
    }

    private static Dictionary<int, (PositionedTextRun Run, bool[] Removed)> SelectIntersectingGlyphs(
        byte[] content, IReadOnlyDictionary<string, ReverseFont>? fonts, IReadOnlyList<PdfRect> regions, ContentTokenizer.Cache cache)
    {
        var selected = new Dictionary<int, (PositionedTextRun Run, bool[] Removed)>();
        foreach (var run in TextSearch.Extract(content, fonts, cache))
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
                removed[i] = IntersectsAny(InkQuad(run, i).Bounds, regions);
                any |= removed[i];
            }

            if (any)
            {
                selected.Add(run.OperatorIndex, (run, removed));
            }
        }

        return selected;
    }

    private static int[] PlanRemovals(IReadOnlyList<ContentElement> content, IReadOnlyList<PdfRect> regions, Func<string, bool> isRemovableXObject)
    {
        var removals = new List<int>();
        for (var i = content.Count - 1; i >= 0; i--)
        {
            switch (content[i])
            {
                case PathContent path when path.GetBounds() is { } bounds && IntersectsAny(bounds, regions):
                    if (path.Clip != PathClipMode.None)
                    {
                        throw new NotSupportedException("A redaction region intersects a clipping path that cannot be removed safely.");
                    }

                    removals.Add(i);
                    break;
                case ImageContent image when IntersectsAny(TransformedBounds(image.Bounds, image.Transform), regions):
                    removals.Add(i);
                    break;
                case XObjectContent xobject when IntersectsAny(UnitBounds(xobject.Transform), regions):
                    if (!isRemovableXObject(xobject.Name))
                    {
                        throw new NotSupportedException($"A redaction region intersects XObject '{xobject.Name}'. Its image or form subtype cannot be determined safely from the content stream.");
                    }

                    removals.Add(i);
                    break;
                case InlineImageContent inline when IntersectsAny(UnitBounds(inline.Transform), regions):
                    removals.Add(i);
                    break;
                case RawContent unmodeled when ContentOperatorClass.MayPaintUnknown(unmodeled.Operator)
                    && (unmodeled.ClipBounds is not { } clip || IntersectsAny(clip, regions)):
                    throw new NotSupportedException($"A redaction region intersects content painted by the '{unmodeled.Operator}' operator. Its extent cannot be determined safely from the content stream.");
                case RawContent:
                    break;
            }
        }

        return [.. removals];
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

    private const double DescentEmFraction = 0.3;

    private static TextQuadrilateral InkQuad(PositionedTextRun run, int index)
        => TextSearch.Quad(
            run.Matrix,
            index,
            1,
            run.AdvanceOffsets,
            Math.Min(run.FontSize, -DescentEmFraction * run.FontSize),
            Math.Max(run.FontSize, -DescentEmFraction * run.FontSize));

    private static bool MayReach(PositionedTextRun run, IReadOnlyList<PdfRect> regions)
    {
        if (!run.Matrix.TryInvert(out var inverse))
        {
            return true;
        }

        var top = Math.Max(run.FontSize, -DescentEmFraction * run.FontSize);
        var bottom = Math.Min(run.FontSize, -DescentEmFraction * run.FontSize);
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
