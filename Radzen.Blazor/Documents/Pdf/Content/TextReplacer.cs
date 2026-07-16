using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

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
    private sealed record Show(int Index, string Operator, Token Text, string? FontName, ReverseFont Font, double FontSize, double Scale, double CharSpacing, double WordSpacing);

    private readonly record struct Edit(int Start, int End, byte[] Bytes);

    private readonly record struct SourceReplacement(TextSourceReference Source, string Replacement);

    public static int Replace(Page page, string search, string replacement, ReplaceTextOptions? options)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(replacement);
        options ??= new ReplaceTextOptions();
        page.ApplyPendingContentEdits();
        var hits = page.FindText(search, options.Search);
        if (hits.Count == 0)
        {
            return 0;
        }

        var content = page.RawContent ?? throw new NotSupportedException("Text replacement requires an existing serialized content stream.");
        var hasMultipleShowMatch = false;
        foreach (var hit in hits)
        {
            hasMultipleShowMatch |= hit.Sources.Count > 1;
        }

        if (hasMultipleShowMatch)
        {
            return ReplaceMultipleShows(page, hits, replacement, options, content);
        }

        var shows = ParseShows(content, page.TextFonts, includeEveryShow: true);
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

        var edits = new List<Edit>(grouped.Count);
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
            var nextOffset = decoded.Length;
            foreach (var reference in group.Value)
            {
                if (reference.CharacterOffset + reference.CharacterLength > nextOffset)
                {
                    throw new InvalidOperationException("Overlapping text matches cannot be replaced safely.");
                }

                oldAdvance += Advance(show.Font, decoded.Substring(reference.CharacterOffset, reference.CharacterLength), show);
                newAdvance += Advance(show.Font, replacement, show);
                changed = changed.Remove(reference.CharacterOffset, reference.CharacterLength).Insert(reference.CharacterOffset, replacement);
                nextOffset = reference.CharacterOffset;
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
                writer.WriteRaw("[");
                writer.WriteString(encoded);
                writer.WriteRaw(" ");
                writer.WriteNumber((newAdvance - oldAdvance) / (show.FontSize * show.Scale) * 1000.0);
                writer.WriteRaw("] TJ");
                edits.Add(new Edit(show.Text.Start, FindOperatorEnd(content, show.Text.End), writer.ToArray()));
            }
            else
            {
                writer.WriteString(encoded);
                edits.Add(new Edit(show.Text.Start, show.Text.End, writer.ToArray()));
            }
        }

        edits.Sort(static (a, b) => b.Start.CompareTo(a.Start));
        var result = content;
        foreach (var edit in edits)
        {
            result = Splice(result, edit);
        }

        page.ApplyEditedContent(result);
        return hits.Count;
    }

    private static int ReplaceMultipleShows(Page page, IReadOnlyList<TextHit> hits, string replacement, ReplaceTextOptions options, byte[] content)
    {
        var shows = ParseShows(content, page.TextFonts, includeEveryShow: true);
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

        var edits = new List<Edit>(grouped.Count);
        foreach (var group in grouped)
        {
            var show = shows[group.Key];
            var decoded = show.Font.Decode(show.Text.Bytes!);
            group.Value.Sort(static (left, right) => left.Source.CharacterOffset.CompareTo(right.Source.CharacterOffset));
            ValidateNonOverlapping(group.Value, decoded);
            edits.Add(BuildMultipleShowEdit(content, show, decoded, group.Value, options.Layout));
        }

        edits.Sort(static (a, b) => b.Start.CompareTo(a.Start));
        var result = content;
        foreach (var edit in edits)
        {
            result = Splice(result, edit);
        }

        page.ApplyEditedContent(result);
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

    private static Edit BuildMultipleShowEdit(byte[] content, Show show, string decoded,
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
            return new Edit(show.Text.Start, show.Text.End, writer.ToArray());
        }

        var denominator = show.FontSize * show.Scale;
        if (!double.IsFinite(denominator) || Math.Abs(denominator) < 0.000001)
        {
            throw new NotSupportedException("Replacing text with a zero or non-finite font scale cannot preserve positioning safely.");
        }

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
            var adjustment = (Advance(show.Font, item.Replacement, show) - Advance(show.Font, oldText, show)) / denominator * 1000.0;
            if (Math.Abs(adjustment) > 0.000001)
            {
                writer.WriteRaw(" ");
                writer.WriteNumber(adjustment);
                writer.WriteRaw(" ");
            }

            offset = item.Source.CharacterOffset + item.Source.CharacterLength;
        }

        if (!show.Font.TryEncode(decoded[offset..], out var trailing))
        {
            throw new NotSupportedException("The source font does not contain every glyph required by the replacement text.");
        }

        writer.WriteString(trailing);
        writer.WriteRaw("] TJ");
        return new Edit(show.Text.Start, FindOperatorEnd(content, show.Text.End), writer.ToArray());
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

    private static void ValidateNonOverlapping(IReadOnlyList<SourceReplacement> replacements, string decoded)
    {
        var nextOffset = 0;
        foreach (var item in replacements)
        {
            ValidateRange(item.Source, decoded);
            if (item.Source.CharacterOffset < nextOffset)
            {
                throw new InvalidOperationException("Overlapping text matches cannot be replaced safely.");
            }

            nextOffset = item.Source.CharacterOffset + item.Source.CharacterLength;
        }
    }

    private static List<Show> ParseShows(byte[] content, IReadOnlyDictionary<string, ReverseFont>? fonts, bool includeEveryShow = false)
    {
        var result = new List<Show>();
        var operands = new List<Token>();
        string? fontName = null;
        var font = ReverseFont.WinAnsi;
        var fontSize = 0.0;
        var scale = 1.0;
        var charSpacing = 0.0;
        var wordSpacing = 0.0;
        foreach (var token in ContentTokenizer.Tokenize(content))
        {
            if (token.Kind is TokenKind.Number or TokenKind.Name or TokenKind.String)
            {
                operands.Add(token);
                continue;
            }

            if (token.Kind != TokenKind.Operator)
            {
                continue;
            }

            switch (token.Text)
            {
                case "Tf":
                    var name = LastName(operands);
                    fontName = name;
                    font = name is not null && fonts is not null && fonts.TryGetValue(name, out var resolved) ? resolved : ReverseFont.WinAnsi;
                    fontSize = LastNumber(operands);
                    break;
                case "Tz": scale = LastNumber(operands) / 100.0; break;
                case "Tc": charSpacing = LastNumber(operands); break;
                case "Tw": wordSpacing = LastNumber(operands); break;
                case "Tj":
                case "'":
                case "\"":
                    var text = LastString(operands);
                    if (text is not null)
                    {
                        result.Add(new Show(result.Count, token.Text, text.Value, fontName, font, fontSize, scale, charSpacing, wordSpacing));
                    }
                    else if (includeEveryShow)
                    {
                        result.Add(new Show(result.Count, token.Text, default, fontName, font, fontSize, scale, charSpacing, wordSpacing));
                    }

                    break;
                case "TJ" when includeEveryShow:
                    result.Add(new Show(result.Count, token.Text, default, fontName, font, fontSize, scale, charSpacing, wordSpacing));
                    break;
            }

            operands.Clear();
        }

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
            if (!font.TryGetWidth(code.Code, out var width))
            {
                throw new NotSupportedException($"The source font does not provide a usable width for character code {code.Code}.");
            }

            value += width / 1000.0 * show.FontSize + show.CharSpacing;
            if (code.IsWordSpace)
            {
                value += show.WordSpacing;
            }
        }

        return value * show.Scale;
    }

    private static int FindOperatorEnd(byte[] content, int afterString)
    {
        var position = afterString;
        while (position < content.Length && content[position] is 0 or 9 or 10 or 12 or 13 or 32)
        {
            position++;
        }

        if (position + 2 > content.Length || content[position] != (byte)'T' || content[position + 1] != (byte)'j')
        {
            throw new FormatException("The source text-show operator is malformed.");
        }

        return position + 2;
    }

    private static byte[] Splice(byte[] source, Edit edit)
    {
        var result = new byte[source.Length - (edit.End - edit.Start) + edit.Bytes.Length];
        source.AsSpan(0, edit.Start).CopyTo(result);
        edit.Bytes.CopyTo(result, edit.Start);
        source.AsSpan(edit.End).CopyTo(result.AsSpan(edit.Start + edit.Bytes.Length));
        return result;
    }

    private static Token? LastString(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.String)
            {
                return operands[i];
            }
        }

        return null;
    }

    private static string? LastName(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Name)
            {
                return operands[i].Text;
            }
        }

        return null;
    }

    private static double LastNumber(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Number)
            {
                return operands[i].Number;
            }
        }

        return 0;
    }
}
