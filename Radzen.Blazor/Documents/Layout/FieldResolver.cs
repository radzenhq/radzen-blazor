using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal sealed class FieldResolver(
    FontCollection fonts,
    LoweringContext resolution,
    LayoutCaptureContext capture)
{
    private static readonly FieldParagraphVisitor fieldParagraphs = new();

    public Paragraph? ParagraphWithFields(Block block) => block.Accept(fieldParagraphs, this);

    private sealed class FieldParagraphVisitor : BlockVisitor<FieldResolver, Paragraph?>
    {
        protected override Paragraph? Default(Block block, FieldResolver resolver) => null;

        public override Paragraph? Visit(Paragraph block, FieldResolver resolver)
            => resolver.HasField(block) ? block : null;
    }

    private bool HasField(Paragraph paragraph)
    {
        foreach (var inline in paragraph.Inlines)
        {
            if (inline is PageNumberField or PageCountField)
            {
                return true;
            }
        }

        return false;
    }

    private Font ResolvedFont(TextInline run) => resolution.RunFont(run) ?? run.Font;

    private bool SameStyle(TextInline a, TextInline b)
    {
        var fontA = ResolvedFont(a);
        var fontB = ResolvedFont(b);
        return a.Link == b.Link
            && a.LinkToAnchor == b.LinkToAnchor
            && a.Anchor == b.Anchor
            && a.LetterSpacing.Equals(b.LetterSpacing)
            && a.VerticalAlignment == b.VerticalAlignment
            && a.VerticalAlignmentScale == b.VerticalAlignmentScale
            && a.Opacity == b.Opacity
            && a.WordSpacing.Equals(b.WordSpacing)
            && a.HorizontalScale == b.HorizontalScale
            && a.Invisible == b.Invisible
            && fontA.Family == fontB.Family
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
        var pieces = new List<(Inline Run, StringBuilder? Text, int TabsBefore)>();
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
                case Run textRun:
                    text = textRun.Text;
                    break;
                default:
                    throw new NotSupportedException(
                        $"ResolveFields cannot resolve inline run of type '{run!.GetType().Name}'.");
            }

            var styled = (TextInline)run;
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

                if (pendingTabs == 0 && pieces.Count > 0 && pieces[^1].Text is { } previous
                    && pieces[^1].Run is TextInline last && SameStyle(last, styled))
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
            LeftIndent = resolution.Format(paragraph).LeftIndent,
            LineSpacing = paragraph.LineSpacing,
            Alignment = paragraph.Alignment,
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
                resolved.Inlines.AddBorrowed(run);
                continue;
            }

            var newRun = new Run(new string('\t', tabsBefore) + builderText.ToString());
            if (run is TextInline source)
            {
                source.CopyPropertiesTo(newRun);
                newRun.Font.InheritFrom(ResolvedFont(source));
            }
            else
            {
                newRun.Font.InheritFrom(resolution.ParagraphFont(paragraph) ?? paragraph.Font);
            }

            resolved.Inlines.Add(newRun);
        }

        var lines = LineLayouter.Layout(
            resolved,
            width,
            fonts,
            capture,
            inheritedAlignment,
            resolution);

        if (lines.Count > reservedLines)
        {
            throw new InvalidOperationException(
                $"A field paragraph wrapped to {lines.Count} lines on page {pageNumber} " +
                $"but only {reservedLines} were reserved; widen the available width or shorten the text.");
        }

        return lines;
    }
}
