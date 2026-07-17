using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf.Content;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

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
            if (!double.IsFinite(area.Left) || !double.IsFinite(area.Bottom) || !double.IsFinite(area.Right) || !double.IsFinite(area.Top)
                || area.Width <= 0 || area.Height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(areas), "Redaction regions must have finite coordinates and positive dimensions.");
            }
        }

        if (regions.Length == 0)
        {
            return;
        }

        if (page.CurrentContent is { Length: > 0 } raw)
        {
            var selected = new Dictionary<int, (PositionedTextRun Run, bool[] Removed)>();
            foreach (var run in page.ExtractPositionedText(cache))
            {
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

            if (selected.Count > 0)
            {
                page.ApplyEditedContent(RemoveTextGlyphs(raw, selected, cache));
            }
        }

        var content = page.Content;
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
                case ImageContent image when IntersectsAny(image.Bounds, regions):
                    content.RemoveAt(i);
                    break;
                case XObjectContent xobject when IntersectsAny(UnitBounds(xobject.Transform), regions):
                    throw new NotSupportedException($"A redaction region intersects XObject '{xobject.Name}'. Its image or form subtype cannot be determined safely from the content stream.");
                case InlineImageContent inline when IntersectsAny(UnitBounds(inline.Transform), regions):
                    content.RemoveAt(i);
                    break;
                // An operator with no modeled shape paints somewhere inside the clip in
                // effect, and nowhere else; an unclipped one can paint anywhere at all.
                case RawContent unmodeled when MayPaint(unmodeled.Operator)
                    && (unmodeled.ClipBounds is not { } clip || IntersectsAny(clip, regions)):
                    throw new NotSupportedException($"A redaction region intersects content painted by the '{unmodeled.Operator}' operator. Its extent cannot be determined safely from the content stream.");
                case RawContent:
                    break;
            }
        }

        if (options?.FillColor is { } fill)
        {
            foreach (var area in regions)
            {
                var overlay = new PathContent { Fill = true, FillColor = fill };
                overlay.MoveTo(area.Left, area.Bottom);
                overlay.LineTo(area.Right, area.Bottom);
                overlay.LineTo(area.Right, area.Top);
                overlay.LineTo(area.Left, area.Top);
                overlay.Close();
                content.Add(overlay);
            }
        }
    }

    private static byte[] RemoveTextGlyphs(byte[] source, IReadOnlyDictionary<int, (PositionedTextRun Run, bool[] Removed)> selected, ContentTokenizer.Cache? cache)
    {
        var edits = new List<ContentEdit>();
        var operandsStart = -1;
        var arrayStart = -1;
        var showIndex = 0;
        foreach (var token in ContentTokenizer.Tokenize(source, cache))
        {
            if (token.Kind is TokenKind.Number or TokenKind.Name or TokenKind.String)
            {
                operandsStart = operandsStart < 0 ? token.Start : operandsStart;
                continue;
            }

            if (token.Kind == TokenKind.ArrayStart)
            {
                arrayStart = token.Start;
                continue;
            }

            if (token.Kind != TokenKind.Operator)
            {
                continue;
            }

            if (ContentShows.IsShow(token.Text))
            {
                if (selected.TryGetValue(showIndex, out var selection))
                {
                    if (token.Text is "'" or "\"")
                    {
                        throw new NotSupportedException($"Redacting text shown by the '{token.Text}' operator cannot preserve line positioning safely.");
                    }

                    var start = token.Text == "TJ" ? arrayStart : operandsStart;
                    if (start < 0)
                    {
                        throw new FormatException("The text-show operator has no valid operand.");
                    }

                    edits.Add(new ContentEdit(start, token.End, BuildRedactedShow(selection.Run, selection.Removed)));
                }

                showIndex++;
            }

            operandsStart = -1;
            arrayStart = -1;
        }

        return ContentEdits.Apply(source, edits);
    }

    private static byte[] BuildRedactedShow(PositionedTextRun run, IReadOnlyList<bool> removed)
    {
        var denominator = run.FontSize * run.Scale;
        if (!double.IsFinite(denominator) || Math.Abs(denominator) < 0.000001)
        {
            throw new NotSupportedException("Redacting text with a zero or non-finite font scale cannot preserve positioning safely.");
        }

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
            if (!run.Font.TryGetWidth(code.Code, out var width))
            {
                throw new NotSupportedException($"The source font does not provide a usable width for character code {code.Code}.");
            }

            writer.WriteString(bytes);
            var nominalAdvance = GlyphMetrics.Advance(width / 1000.0, run.FontSize, run.CharSpacing, run.WordSpacing, code.IsWordSpace) * run.Scale;
            WriteAdvance(writer, desiredAdvance - nominalAdvance, denominator);
        }

        writer.WriteRaw("] TJ");
        return writer.ToArray();
    }

    private static void WriteAdvance(ContentWriter writer, double advance, double denominator)
    {
        if (Math.Abs(advance) > 0.000001)
        {
            writer.WriteRaw(" ");
            writer.WriteNumber(-advance / denominator * 1000.0);
            writer.WriteRaw(" ");
        }
    }

    // Every unmodeled operator is assumed to put marks on the page unless it is one of the
    // few known to only mutate graphics state or annotate the stream. Guessing the other
    // way round would let an unrecognised painting operator survive a redaction silently.
    private static bool MayPaint(string op) => op is not ("gs" or "ri" or "i" or "j" or "J" or "M"
        or "BX" or "EX" or "MP" or "DP" or "d0" or "d1");

    private static PdfRect UnitBounds(Matrix transform)
    {
        var points = new[] { transform.Transform(0, 0), transform.Transform(1, 0), transform.Transform(1, 1), transform.Transform(0, 1) };
        return new PdfRect(points.Min(static p => p.X), points.Min(static p => p.Y), points.Max(static p => p.X), points.Max(static p => p.Y));
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
