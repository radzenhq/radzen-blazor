using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;

namespace Radzen.Documents.Pdf;

/// <summary>Specifies how replacement text affects the text advance.</summary>
public enum TextReplacementLayout
{
    /// <summary>Preserves the original total advance so following content does not move.</summary>
    PreserveAdvance,

    /// <summary>Rejects a replacement that is wider than the matched text.</summary>
    FailIfWider,

    /// <summary>Allows the replacement to change the advance of following text.</summary>
    AllowAdvance,
}

/// <summary>Specifies text matching and layout behavior for replacement.</summary>
public sealed class ReplaceTextOptions
{
    /// <summary>Gets or sets the text matching options.</summary>
    public TextSearchOptions Search { get; set; } = new();

    /// <summary>Gets or sets the replacement layout policy.</summary>
    public TextReplacementLayout Layout { get; set; } = TextReplacementLayout.PreserveAdvance;
}

internal static class TextReplacer
{
    private sealed record Show(int Index, string Operator, Token Text, int OperatorEnd, string? FontName, ReverseFont Font, double FontSize, double Scale, double CharSpacing, double WordSpacing);

    private readonly record struct SourceReplacement(TextSourceReference Source, string Replacement);

    public static int Replace(Page page, string search, string replacement, ReplaceTextOptions? options)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(replacement);
        options ??= new ReplaceTextOptions();
        var cache = new ContentTokenizer.Cache();
        var hits = page.FindText(search, options.Search, -1, cache);
        if (hits.Count == 0)
        {
            return 0;
        }

        var content = page.CurrentContent ?? throw new NotSupportedException("Text replacement requires an existing serialized content stream.");
        var hasMultipleShowMatch = false;
        foreach (var hit in hits)
        {
            hasMultipleShowMatch |= hit.Sources.Count > 1;
        }

        if (hasMultipleShowMatch)
        {
            return ReplaceMultipleShows(page, hits, replacement, options, content, cache);
        }

        var shows = ParseShows(content, page.TextFonts, cache);
        var grouped = new Dictionary<int, List<TextSourceReference>>();
        foreach (var hit in hits)
        {
            if (hit.Sources.Count != 1)
            {
                throw new NotSupportedException("A text match spanning multiple show operators cannot be replaced safely.");
            }

            var source = hit.Sources[0];
            if (!grouped.TryGetValue(source.OperatorIndex, out var references))
            {
                references = [];
                grouped.Add(source.OperatorIndex, references);
            }

            references.Add(source);
        }

        var edits = new List<ContentEdit>(grouped.Count);
        foreach (var group in grouped)
        {
            var source = group.Value[0];
            var show = GetShow(shows, source);
            ValidateTj(show);
            if (!show.Font.TryEncode(replacement, out _))
            {
                throw new NotSupportedException("The source font does not contain every glyph required by the replacement text.");
            }

            var decoded = show.Font.Decode(show.Text.Bytes!);
            var changed = decoded;
            var oldAdvance = 0.0;
            var newAdvance = 0.0;
            group.Value.Sort(static (left, right) => right.CharacterOffset.CompareTo(left.CharacterOffset));
            ValidateNonOverlapping([.. Enumerable.Reverse(group.Value)], decoded);
            foreach (var reference in group.Value)
            {
                oldAdvance += Advance(show.Font, decoded.Substring(reference.CharacterOffset, reference.CharacterLength), show);
                newAdvance += Advance(show.Font, replacement, show);
                changed = changed.Remove(reference.CharacterOffset, reference.CharacterLength).Insert(reference.CharacterOffset, replacement);
            }

            if (!show.Font.TryEncode(changed, out var encoded))
            {
                throw new NotSupportedException("The source font does not contain every glyph required by the replacement text.");
            }

            if (options.Layout == TextReplacementLayout.FailIfWider && newAdvance > oldAdvance + 0.000001)
            {
                throw new InvalidOperationException("The replacement text is wider than the matched text.");
            }

            using var writer = new ContentWriter();
            if (options.Layout == TextReplacementLayout.PreserveAdvance && Math.Abs(newAdvance - oldAdvance) > 0.000001)
            {
                var denominator = ContentWriter.RequireTjScale(show.FontSize, show.Scale, "Replacing");

                writer.WriteRaw("[");
                writer.WriteString(encoded);
                writer.WriteTjAdjustment(newAdvance - oldAdvance, denominator);
                writer.WriteRaw("] TJ");
                edits.Add(new ContentEdit(show.Text.Start, show.OperatorEnd, writer.ToArray()));
            }
            else
            {
                writer.WriteString(encoded);
                edits.Add(new ContentEdit(show.Text.Start, show.Text.End, writer.ToArray()));
            }
        }

        page.ApplyEditedContent(ContentEdits.Apply(content, edits));
        return hits.Count;
    }

    private static int ReplaceMultipleShows(Page page, IReadOnlyList<TextHit> hits, string replacement, ReplaceTextOptions options, byte[] content, ContentTokenizer.Cache? cache)
    {
        var shows = ParseShows(content, page.TextFonts, cache);
        var grouped = new Dictionary<int, List<SourceReplacement>>();
        foreach (var hit in hits)
        {
            if (hit.Sources.Count == 0)
            {
                throw new NotSupportedException("A text match without source glyphs cannot be replaced safely.");
            }

            var first = GetShow(shows, hit.Sources[0]);
            ValidateTj(first);
            var sourceReplacements = GetSourceReplacements(hit, replacement, first.Font);

            var oldAdvance = 0.0;
            for (var i = 0; i < hit.Sources.Count; i++)
            {
                var source = hit.Sources[i];
                var show = GetShow(shows, source);
                ValidateTj(show);
                if (i > 0 && source.OperatorIndex != hit.Sources[i - 1].OperatorIndex + 1)
                {
                    throw new NotSupportedException("A text match must span contiguous text-show operators to be replaced safely.");
                }

                if (!SameTextState(first, show))
                {
                    throw new NotSupportedException("A text match spanning multiple show operators must use the same font and text state.");
                }

                var decoded = show.Font.Decode(show.Text.Bytes!);
                ValidateRange(source, decoded);
                oldAdvance += Advance(show.Font, decoded.Substring(source.CharacterOffset, source.CharacterLength), show);
                if (!grouped.TryGetValue(source.OperatorIndex, out var replacements))
                {
                    replacements = [];
                    grouped.Add(source.OperatorIndex, replacements);
                }

                replacements.Add(new SourceReplacement(source, sourceReplacements[i]));
            }

            if (options.Layout == TextReplacementLayout.FailIfWider
                && ReplacementAdvance(shows, hit.Sources, sourceReplacements) > oldAdvance + 0.000001)
            {
                throw new InvalidOperationException("The replacement text is wider than the matched text.");
            }
        }

        var edits = new List<ContentEdit>(grouped.Count);
        foreach (var group in grouped)
        {
            var show = shows[group.Key];
            var decoded = show.Font.Decode(show.Text.Bytes!);
            group.Value.Sort(static (left, right) => left.Source.CharacterOffset.CompareTo(right.Source.CharacterOffset));
            ValidateNonOverlapping([.. group.Value.Select(static item => item.Source)], decoded);
            edits.Add(BuildMultipleShowEdit(show, decoded, group.Value, options.Layout));
        }

        page.ApplyEditedContent(ContentEdits.Apply(content, edits));
        return hits.Count;
    }

    private static IReadOnlyList<string> GetSourceReplacements(TextHit hit, string replacement, ReverseFont font)
    {
        if (font.TryEncode(replacement, out _))
        {
            var direct = new string[hit.Sources.Count];
            direct[0] = replacement;
            Array.Fill(direct, string.Empty, 1, direct.Length - 1);
            return direct;
        }

        if (hit.SyntheticGapBoundaries.Count != hit.Sources.Count - 1)
        {
            throw new NotSupportedException("The replacement cannot be aligned to the source text-show operators safely.");
        }

        foreach (var boundary in hit.SyntheticGapBoundaries)
        {
            if (!boundary)
            {
                throw new NotSupportedException("The replacement cannot be aligned because every source operator boundary is not a synthetic positioning gap.");
            }
        }

        var segments = SplitAtWhitespace(replacement);
        if (segments.Count != hit.Sources.Count)
        {
            throw new NotSupportedException("The replacement must contain one whitespace boundary for each synthetic positioning gap in the matched text.");
        }

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                throw new NotSupportedException("The replacement must contain non-whitespace text on both sides of every synthetic positioning gap.");
            }

            if (!font.TryEncode(segment, out _))
            {
                throw new NotSupportedException("The source font does not contain every glyph required by its replacement segment.");
            }
        }

        return segments;
    }

    private static List<string> SplitAtWhitespace(string replacement)
    {
        var segments = new List<string>();
        var start = 0;
        for (var i = 0; i < replacement.Length;)
        {
            if (!char.IsWhiteSpace(replacement[i]))
            {
                i++;
                continue;
            }

            segments.Add(replacement[start..i]);
            do
            {
                i++;
            }
            while (i < replacement.Length && char.IsWhiteSpace(replacement[i]));

            start = i;
        }

        segments.Add(replacement[start..]);
        return segments;
    }

    private static double ReplacementAdvance(IReadOnlyList<Show> shows, IReadOnlyList<TextSourceReference> sources,
        IReadOnlyList<string> replacements)
    {
        var advance = 0.0;
        for (var i = 0; i < sources.Count; i++)
        {
            var show = GetShow(shows, sources[i]);
            advance += Advance(show.Font, replacements[i], show);
        }

        return advance;
    }

    private static ContentEdit BuildMultipleShowEdit(Show show, string decoded,
        IReadOnlyList<SourceReplacement> replacements, TextReplacementLayout layout)
    {
        using var writer = new ContentWriter();
        if (layout != TextReplacementLayout.PreserveAdvance)
        {
            var changed = decoded;
            for (var i = replacements.Count - 1; i >= 0; i--)
            {
                var item = replacements[i];
                changed = changed.Remove(item.Source.CharacterOffset, item.Source.CharacterLength)
                    .Insert(item.Source.CharacterOffset, item.Replacement);
            }

            if (!show.Font.TryEncode(changed, out var encoded))
            {
                throw new NotSupportedException("The source font does not contain every glyph required by the replacement text.");
            }

            writer.WriteString(encoded);
            return new ContentEdit(show.Text.Start, show.Text.End, writer.ToArray());
        }

        var denominator = ContentWriter.RequireTjScale(show.FontSize, show.Scale, "Replacing");

        writer.WriteRaw("[");
        var offset = 0;
        foreach (var item in replacements)
        {
            var prefix = decoded[offset..item.Source.CharacterOffset] + item.Replacement;
            if (!show.Font.TryEncode(prefix, out var encoded))
            {
                throw new NotSupportedException("The source font does not contain every glyph required by the replacement text.");
            }

            writer.WriteString(encoded);
            var oldText = decoded.Substring(item.Source.CharacterOffset, item.Source.CharacterLength);
            writer.WriteTjAdjustment(
                Advance(show.Font, item.Replacement, show) - Advance(show.Font, oldText, show), denominator);
            offset = item.Source.CharacterOffset + item.Source.CharacterLength;
        }

        if (!show.Font.TryEncode(decoded[offset..], out var trailing))
        {
            throw new NotSupportedException("The source font does not contain every glyph required by the replacement text.");
        }

        writer.WriteString(trailing);
        writer.WriteRaw("] TJ");
        return new ContentEdit(show.Text.Start, show.OperatorEnd, writer.ToArray());
    }

    private static Show GetShow(IReadOnlyList<Show> shows, TextSourceReference source)
    {
        if (source.OperatorIndex < 0 || source.OperatorIndex >= shows.Count)
        {
            throw new InvalidOperationException("The text match source operator is unavailable.");
        }

        return shows[source.OperatorIndex];
    }

    private static void ValidateTj(Show show)
    {
        if (show.Operator != "Tj")
        {
            throw new NotSupportedException($"Replacing text in the '{show.Operator}' show operator is not supported safely.");
        }

        if (show.Text.Bytes is null)
        {
            throw new FormatException("The source text-show operator has no valid string operand.");
        }
    }

    private static bool SameTextState(Show first, Show current)
        => string.Equals(first.FontName, current.FontName, StringComparison.Ordinal)
        && ReferenceEquals(first.Font, current.Font)
        && first.FontSize == current.FontSize
        && first.Scale == current.Scale
        && first.CharSpacing == current.CharSpacing
        && first.WordSpacing == current.WordSpacing;

    private static void ValidateRange(TextSourceReference source, string decoded)
    {
        if (source.CharacterOffset < 0 || source.CharacterLength < 0
            || source.CharacterOffset > decoded.Length - source.CharacterLength)
        {
            throw new InvalidOperationException("The text match source range is unavailable.");
        }
    }

    private static void ValidateNonOverlapping(IReadOnlyList<TextSourceReference> sources, string decoded)
    {
        var nextOffset = 0;
        foreach (var source in sources)
        {
            ValidateRange(source, decoded);
            if (source.CharacterOffset < nextOffset)
            {
                throw new InvalidOperationException("Overlapping text matches cannot be replaced safely.");
            }

            nextOffset = source.CharacterOffset + source.CharacterLength;
        }
    }

    private static List<Show> ParseShows(byte[] content, IReadOnlyDictionary<string, ReverseFont>? fonts, ContentTokenizer.Cache? cache)
    {
        var result = new List<Show>();
        ContentTextWalker.Walk(content, fonts, (walker, op, operands, array, operatorIndex) =>
        {
            result.Add(new Show(operatorIndex, op, op == "TJ" ? default : LastStringToken(operands) ?? default,
                walker.Operator.End, walker.FontName, walker.Font ?? ReverseFont.WinAnsi, walker.FontSize,
                walker.HorizontalScale, walker.CharSpacing, walker.WordSpacing));
            return 0.0;
        }, cache);

        return result;
    }

    private static double Advance(ReverseFont font, string text, Show show)
    {
        if (!font.TryEncode(text, out var codes))
        {
            return double.PositiveInfinity;
        }

        var value = 0.0;
        foreach (var code in font.DecodeCodes(codes))
        {
            value += LoadedGlyphAdvance.Calculate(
                font, code.Code, code.IsWordSpace, show.FontSize, show.Scale,
                show.CharSpacing, show.WordSpacing, MissingWidthPolicy.Throw, out _);
        }

        return value;
    }

}
