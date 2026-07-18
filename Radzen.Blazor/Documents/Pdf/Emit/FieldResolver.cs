using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class FieldResolver(FontCollection fonts, StyleResolution resolution)
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

    private Font ResolvedFont(Run run) => resolution.RunFont(run) ?? run.Font;

    private bool SameStyle(Run a, Run b)
    {
        var fontA = ResolvedFont(a);
        var fontB = ResolvedFont(b);
        return a.Link == b.Link
            && a.LinkToAnchor == b.LinkToAnchor
            && a.Anchor == b.Anchor
            && a.LetterSpacing.Equals(b.LetterSpacing)
            && a.VerticalAlign == b.VerticalAlign
            && a.VerticalAlignScale == b.VerticalAlignScale
            && a.Opacity == b.Opacity
            && a.WordSpacing.Equals(b.WordSpacing)
            && a.HorizontalScale == b.HorizontalScale
            && a.Invisible == b.Invisible
            && Equals(a.FillPaint, b.FillPaint)
            && fontA.Name == fontB.Name
            && fontA.Size == fontB.Size
            && fontA.Bold == fontB.Bold
            && fontA.Italic == fontB.Italic
            && fontA.Underline == fontB.Underline
            && fontA.Strikethrough == fontB.Strikethrough
            && fontA.Color.Equals(fontB.Color);
    }

    public IReadOnlyList<LineBox> ResolveFields(
        Paragraph paragraph,
        double width,
        int pageNumber,
        int pageCount,
        HorizontalAlignment? inheritedAlignment,
        int reservedLines)
    {
        var pieces = new List<(Run Run, StringBuilder? Text, int TabsBefore)>();
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
                    if (pendingTabs > 0)
                    {
                        pieces.Add((run, new StringBuilder(), pendingTabs));
                        pendingTabs = 0;
                    }

                    pieces.Add((run, null, 0));
                    continue;
                case { } when run.GetType() == typeof(Run):
                    text = run.Text;
                    break;
                default:
                    throw new NotSupportedException(
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
                    pieces.Add((run, new StringBuilder(part), pendingTabs));
                    pendingTabs = 0;
                }
            }
        }

        if (pendingTabs > 0 && pieces.Count > 0)
        {
            pieces.Add((pieces[^1].Run, new StringBuilder(), pendingTabs));
        }

        var resolved = new Paragraph
        {
            LeftIndent = paragraph.LeftIndent,
            LineSpacing = paragraph.LineSpacing,
            RightTabStop = paragraph.RightTabStop,
            AlignmentValue = paragraph.AlignmentValue,
        };
        resolved.Font.InheritFrom(resolution.ParagraphFont(paragraph) ?? paragraph.Font);

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

            var newRun = new Run(new string('\t', tabsBefore) + builderText.ToString());
            run.CopyPropertiesTo(newRun);
            newRun.Font.InheritFrom(ResolvedFont(run));
            resolved.Inlines.Add(newRun);
        }

        var lines = LineBreaker.Break(resolved, width, fonts, inheritedAlignment, resolution);

        if (lines.Count > reservedLines)
        {
            throw new InvalidOperationException(
                $"A field paragraph wrapped to {lines.Count} lines on page {pageNumber} " +
                $"but only {reservedLines} were reserved; widen the available width or shorten the text.");
        }

        return lines;
    }
}
