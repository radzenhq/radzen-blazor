using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf;

// Resolves page-number/count fields at emit time: detects paragraphs that carry fields
// and re-breaks them with the actual page number/count substituted, so a band laid out
// once per section renders the right value on every page.
internal sealed class FieldResolver(FontCollection fonts)
{
    public bool HasField(Paragraph paragraph)
    {
        foreach (var run in paragraph.Inlines)
        {
            if (run is PageNumberField or PageCountField)
            {
                return true;
            }
        }

        return false;
    }

    private static bool SameStyle(Run a, Run b)
    {
        var fontA = a.ResolvedFont;
        var fontB = b.ResolvedFont;
        return a.Link == b.Link
            && fontA.Name == fontB.Name
            && fontA.Size == fontB.Size
            && fontA.Bold == fontB.Bold
            && fontA.Italic == fontB.Italic
            && fontA.Underline == fontB.Underline
            && fontA.Strikethrough == fontB.Strikethrough
            && fontA.Color.Equals(fontB.Color);
    }

    // Substitutes page-number/count fields with their resolved text, then re-runs the
    // line breaker so the paragraph wraps, honors '\n' forced breaks and tab stops, and
    // emits ALL of its lines. Consecutive runs of the same style merge into one piece so
    // the field text keeps its inter-word spaces intact (the breaker re-inserts the gaps
    // and CoalesceFragments restores them at emit); tabs are re-encoded as '\t' between
    // pieces so a trailing tab is preserved rather than dropped.
    public IReadOnlyList<LineBox> ResolveFields(
        Paragraph paragraph,
        double width,
        int pageNumber,
        int pageCount,
        HorizontalAlignment? inheritedAlignment = null,
        int reservedLines = int.MaxValue)
    {
        // Text == null marks a passthrough piece: a non-text run (e.g. InlineImage) re-emitted
        // as its ORIGINAL instance so the line breaker lays it out instead of coercing it to
        // empty text.
        var pieces = new List<(Run Run, System.Text.StringBuilder? Text, int TabsBefore)>();
        var pendingTabs = 0;
        foreach (var run in paragraph.Inlines)
        {
            string text;
            switch (run)
            {
                case PageNumberField:
                    text = pageNumber.ToString(CultureInfo.InvariantCulture);
                    break;
                case PageCountField:
                    text = pageCount.ToString(CultureInfo.InvariantCulture);
                    break;
                case InlineImage:
                    // Flush a pending tab so the image keeps its tab-stop position, then re-emit
                    // the image itself rather than dropping it as its (empty) Text.
                    if (pendingTabs > 0)
                    {
                        pieces.Add((run, new System.Text.StringBuilder(), pendingTabs));
                        pendingTabs = 0;
                    }

                    pieces.Add((run, null, 0));
                    continue;
                case { } when run.GetType() == typeof(Run):
                    text = run.Text;
                    break;
                default:
                    throw new System.NotSupportedException(
                        $"ResolveFields cannot resolve inline run of type '{run!.GetType().Name}'.");
            }

            var parts = text.Split('\t');
            for (var pi = 0; pi < parts.Length; pi++)
            {
                if (pi > 0)
                {
                    pendingTabs++;
                }

                var part = parts[pi];
                if (part.Length == 0)
                {
                    continue;
                }

                if (pendingTabs == 0 && pieces.Count > 0 && pieces[^1].Text is { } previous && SameStyle(pieces[^1].Run, run))
                {
                    previous.Append(part);
                }
                else
                {
                    pieces.Add((run, new System.Text.StringBuilder(part), pendingTabs));
                    pendingTabs = 0;
                }
            }
        }

        // A trailing tab (no piece follows it) survives as a final tabs-only piece.
        if (pendingTabs > 0 && pieces.Count > 0)
        {
            pieces.Add((pieces[^1].Run, new System.Text.StringBuilder(), pendingTabs));
        }

        var resolved = new Paragraph
        {
            LeftIndent = paragraph.LeftIndent,
            LineSpacing = paragraph.LineSpacing,
            RightTabStop = paragraph.RightTabStop,
            AlignmentValue = paragraph.AlignmentValue,
            EffectiveFont = paragraph.EffectiveFont,
        };

        foreach (var stop in paragraph.TabStops)
        {
            resolved.TabStops.Add(stop);
        }

        foreach (var (run, builderText, tabsBefore) in pieces)
        {
            if (builderText is null)
            {
                resolved.Inlines.Add(run);
                continue;
            }

            resolved.Inlines.Add(new Run(new string('\t', tabsBefore) + builderText.ToString())
            {
                EffectiveFont = run.ResolvedFont,
                Link = run.Link,
            });
        }

        var lines = LineBreaker.Break(resolved, width, fonts, inheritedAlignment);

        // The band reserved exactly `reservedLines` for this paragraph (laid out once with the
        // field's single-digit placeholder). A wider resolved number that wraps to more lines
        // would overprint the content below it, so fail loud instead of drawing over it.
        if (lines.Count > reservedLines)
        {
            throw new System.InvalidOperationException(
                $"A header/footer field paragraph wrapped to {lines.Count} lines on page {pageNumber} " +
                $"but only {reservedLines} were reserved; widen the band or shorten the text.");
        }

        return lines;
    }
}
