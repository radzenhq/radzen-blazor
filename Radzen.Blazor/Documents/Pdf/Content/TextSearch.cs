using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a point in PDF user space.</summary>
/// <param name="x">The horizontal coordinate.</param>
/// <param name="y">The vertical coordinate.</param>
public readonly struct TextPoint(double x, double y)
{
    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; } = x;

    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; } = y;
}

/// <summary>Represents an axis-aligned bounding box in PDF user space.</summary>
/// <param name="left">The minimum horizontal coordinate.</param>
/// <param name="bottom">The minimum vertical coordinate.</param>
/// <param name="right">The maximum horizontal coordinate.</param>
/// <param name="top">The maximum vertical coordinate.</param>
public readonly struct TextBounds(double left, double bottom, double right, double top)
{
    /// <summary>Gets the minimum horizontal coordinate.</summary>
    public double Left { get; } = left;

    /// <summary>Gets the minimum vertical coordinate.</summary>
    public double Bottom { get; } = bottom;

    /// <summary>Gets the maximum horizontal coordinate.</summary>
    public double Right { get; } = right;

    /// <summary>Gets the maximum vertical coordinate.</summary>
    public double Top { get; } = top;

    /// <summary>Gets the width.</summary>
    public double Width => Right - Left;

    /// <summary>Gets the height.</summary>
    public double Height => Top - Bottom;
}

/// <summary>Represents a transformed text quadrilateral in PDF user space.</summary>
/// <param name="lowerLeft">The lower-left text-space corner after transformation.</param>
/// <param name="lowerRight">The lower-right text-space corner after transformation.</param>
/// <param name="upperRight">The upper-right text-space corner after transformation.</param>
/// <param name="upperLeft">The upper-left text-space corner after transformation.</param>
public readonly struct TextQuadrilateral(TextPoint lowerLeft, TextPoint lowerRight, TextPoint upperRight, TextPoint upperLeft)
{
    /// <summary>Gets the transformed lower-left corner.</summary>
    public TextPoint LowerLeft { get; } = lowerLeft;

    /// <summary>Gets the transformed lower-right corner.</summary>
    public TextPoint LowerRight { get; } = lowerRight;

    /// <summary>Gets the transformed upper-right corner.</summary>
    public TextPoint UpperRight { get; } = upperRight;

    /// <summary>Gets the transformed upper-left corner.</summary>
    public TextPoint UpperLeft { get; } = upperLeft;

    /// <summary>Gets the axis-aligned bounds enclosing the quadrilateral.</summary>
    public TextBounds Bounds => TextSearch.GetBounds([LowerLeft, LowerRight, UpperRight, UpperLeft]);
}

/// <summary>Identifies characters within a text-show operator in the page content stream.</summary>
/// <param name="operatorIndex">The zero-based ordinal of the text-show operator.</param>
/// <param name="characterOffset">The UTF-16 offset in the operator's decoded text.</param>
/// <param name="characterLength">The UTF-16 length in the operator's decoded text.</param>
public readonly struct TextSourceReference(int operatorIndex, int characterOffset, int characterLength)
{
    /// <summary>Gets the zero-based ordinal of the text-show operator.</summary>
    public int OperatorIndex { get; } = operatorIndex;

    /// <summary>Gets the UTF-16 offset in the operator's decoded text.</summary>
    public int CharacterOffset { get; } = characterOffset;

    /// <summary>Gets the UTF-16 length in the operator's decoded text.</summary>
    public int CharacterLength { get; } = characterLength;
}

/// <summary>Represents decoded text and geometry from one text-show operator.</summary>
public sealed class PositionedTextRun
{
    internal PositionedTextRun(string text, int operatorIndex, TextQuadrilateral quadrilateral, double[] advanceOffsets,
        ReverseFont font, double fontSize, double scale, double charSpacing, double wordSpacing, Matrix matrix)
    {
        Text = text;
        OperatorIndex = operatorIndex;
        Quadrilateral = quadrilateral;
        AdvanceOffsets = advanceOffsets;
        Font = font;
        FontSize = fontSize;
        Scale = scale;
        CharSpacing = charSpacing;
        WordSpacing = wordSpacing;
        Matrix = matrix;
    }

    /// <summary>Gets the decoded text.</summary>
    public string Text { get; }

    /// <summary>Gets the zero-based ordinal of the source text-show operator.</summary>
    public int OperatorIndex { get; }

    /// <summary>Gets the transformed em-box quadrilateral.</summary>
    public TextQuadrilateral Quadrilateral { get; }

    /// <summary>Gets the axis-aligned bounds enclosing the quadrilateral.</summary>
    public TextBounds Bounds => Quadrilateral.Bounds;

    internal double Advance => AdvanceOffsets[^1];

    internal double[] AdvanceOffsets { get; }

    internal ReverseFont Font { get; }

    internal double FontSize { get; }

    internal double Scale { get; }

    internal double CharSpacing { get; }

    internal double WordSpacing { get; }

    internal Matrix Matrix { get; }

    internal TextQuadrilateral CharacterQuadrilateral(int index) => TextSearch.Quad(Matrix, index, 1, AdvanceOffsets, FontSize);
}

/// <summary>Specifies text-search matching behavior.</summary>
public sealed class TextSearchOptions
{
    /// <summary>Gets or sets whether matching distinguishes uppercase and lowercase text.</summary>
    public bool CaseSensitive { get; set; }

    /// <summary>Gets or sets whether matches must be bounded by non-word characters.</summary>
    public bool WholeWord { get; set; }

    /// <summary>Gets or sets whether consecutive whitespace is matched as one space.</summary>
    public bool NormalizeWhitespace { get; set; }
}

/// <summary>Represents one positioned text-search match.</summary>
public sealed class TextHit
{
    internal TextHit(string text, int pageIndex, IReadOnlyList<TextQuadrilateral> quadrilaterals, IReadOnlyList<TextSourceReference> sources,
        IReadOnlyList<bool> syntheticGapBoundaries)
    {
        Text = text;
        PageIndex = pageIndex;
        Quadrilaterals = quadrilaterals;
        Sources = sources;
        SyntheticGapBoundaries = syntheticGapBoundaries;
        Bounds = TextSearch.GetBounds(quadrilaterals);
    }

    /// <summary>Gets the text as it appears in the extracted page text.</summary>
    public string Text { get; }

    /// <summary>Gets the zero-based page index, or -1 when searching a page directly.</summary>
    public int PageIndex { get; }

    /// <summary>Gets one quadrilateral for each intersected text-show run.</summary>
    public IReadOnlyList<TextQuadrilateral> Quadrilaterals { get; }

    /// <summary>Gets the axis-aligned bounds enclosing all match quadrilaterals.</summary>
    public TextBounds Bounds { get; }

    /// <summary>Gets the source text-show operator references covered by the match.</summary>
    public IReadOnlyList<TextSourceReference> Sources { get; }

    internal IReadOnlyList<bool> SyntheticGapBoundaries { get; }
}

internal static class TextSearch
{
    private const double AverageGlyphEm = 0.5;
    private const double LineTolerance = 0.5;
    private const double SpaceGapEm = 0.2;
    private const double TjSpaceThreshold = 200.0;

    public static IReadOnlyList<PositionedTextRun> Extract(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts)
    {
        var runs = Parse(content, fonts);
        Sort(runs);
        return runs;
    }

    public static IReadOnlyList<TextHit> Find(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts, string text, TextSearchOptions? options, int pageIndex)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(text));
        }

        options ??= new TextSearchOptions();
        var runs = Parse(content, fonts);
        Sort(runs);
        var composed = Compose(runs);
        var needle = options.NormalizeWhitespace ? Normalize(text).Text : text;
        if (needle.Length == 0)
        {
            throw new ArgumentException("Search text must contain a non-whitespace character.", nameof(text));
        }

        var searchable = options.NormalizeWhitespace ? Normalize(composed) : composed;
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var hits = new List<TextHit>();
        var offset = 0;
        while (offset <= searchable.Text.Length - needle.Length)
        {
            var index = searchable.Text.IndexOf(needle, offset, comparison);
            if (index < 0)
            {
                break;
            }

            if (!options.WholeWord || IsWholeWord(searchable.Text, index, needle.Length))
            {
                hits.Add(CreateHit(composed, searchable, runs, index, needle.Length, pageIndex));
            }

            offset = index + Math.Max(needle.Length, 1);
        }

        return hits;
    }

    public static TextBounds GetBounds(IReadOnlyList<TextQuadrilateral> quadrilaterals)
    {
        if (quadrilaterals.Count == 0)
        {
            return new TextBounds();
        }

        var points = new List<TextPoint>(quadrilaterals.Count * 4);
        foreach (var quad in quadrilaterals)
        {
            points.Add(quad.LowerLeft);
            points.Add(quad.LowerRight);
            points.Add(quad.UpperRight);
            points.Add(quad.UpperLeft);
        }

        return GetBounds(points);
    }

    public static TextBounds GetBounds(IReadOnlyList<TextPoint> points)
    {
        if (points.Count == 0)
        {
            return new TextBounds();
        }

        var left = points[0].X;
        var right = left;
        var bottom = points[0].Y;
        var top = bottom;
        for (var i = 1; i < points.Count; i++)
        {
            left = Math.Min(left, points[i].X);
            right = Math.Max(right, points[i].X);
            bottom = Math.Min(bottom, points[i].Y);
            top = Math.Max(top, points[i].Y);
        }

        return new TextBounds(left, bottom, right, top);
    }

    private static List<PositionedTextRun> Parse(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts)
    {
        if (content is null || content.Length == 0)
        {
            return [];
        }

        var runs = new List<PositionedTextRun>();
        ContentTextWalker.Walk(content, fonts, (walker, op, operands, array, operatorIndex) => op == "TJ"
            ? ShowArray(runs, array, walker, operatorIndex)
            : Show(runs, operands, walker, operatorIndex));
        return runs;
    }

    private static double Show(List<PositionedTextRun> runs, List<Token> operands, ContentTextWalker walker, int operatorIndex)
    {
        var bytes = LastString(operands);
        if (bytes is null || bytes.Length == 0)
        {
            return 0.0;
        }

        var reverse = walker.Font ?? ReverseFont.WinAnsi;
        var builder = new StringBuilder();
        var advanceOffsets = new List<double> { 0.0 };
        AppendText(builder, advanceOffsets, reverse, bytes, walker.FontSize, walker.HorizontalScale, walker.CharSpacing, walker.WordSpacing);
        AddRun(runs, builder.ToString(), walker, reverse, advanceOffsets, operatorIndex);
        return advanceOffsets[^1];
    }

    private static double ShowArray(List<PositionedTextRun> runs, List<Token> array, ContentTextWalker walker, int operatorIndex)
    {
        var reverse = walker.Font ?? ReverseFont.WinAnsi;
        var fontSize = walker.FontSize;
        var scale = walker.HorizontalScale;
        var builder = new StringBuilder();
        var advanceOffsets = new List<double> { 0.0 };
        foreach (var element in array)
        {
            if (element.Kind == TokenKind.String && element.Bytes is { Length: > 0 } bytes)
            {
                AppendText(builder, advanceOffsets, reverse, bytes, fontSize, scale, walker.CharSpacing, walker.WordSpacing);
            }
            else if (element.Kind == TokenKind.Number)
            {
                var advance = advanceOffsets[^1] - element.Number / 1000.0 * fontSize * scale;
                if (element.Number <= -TjSpaceThreshold)
                {
                    builder.Append(' ');
                    advanceOffsets.Add(advance);
                }
                else
                {
                    advanceOffsets[^1] = advance;
                }
            }
        }

        AddRun(runs, builder.ToString(), walker, reverse, advanceOffsets, operatorIndex);
        return advanceOffsets[^1];
    }

    private static void AppendText(StringBuilder builder, List<double> advanceOffsets, ReverseFont font, byte[] bytes, double fontSize, double scale, double charSpacing, double wordSpacing)
    {
        var advance = advanceOffsets[^1];
        foreach (var code in font.DecodeCodes(bytes))
        {
            var widthEm = font.TryGetWidth(code.Code, out var width) ? width / 1000.0 : AverageGlyphEm;
            var glyphAdvance = GlyphMetrics.Advance(widthEm, fontSize, charSpacing, wordSpacing, code.IsWordSpace);
            for (var i = 0; i < code.Text.Length; i++)
            {
                builder.Append(code.Text[i]);
                if (i == code.Text.Length - 1)
                {
                    advance += glyphAdvance * scale;
                }

                advanceOffsets.Add(advance);
            }
        }
    }

    private static void AddRun(List<PositionedTextRun> runs, string text, ContentTextWalker walker, ReverseFont font,
        List<double> advanceOffsets, int operatorIndex)
    {
        if (text.Length == 0)
        {
            return;
        }

        if (advanceOffsets.Count != text.Length + 1)
        {
            throw new InvalidOperationException("Text advance offsets do not align with the decoded text.");
        }

        var offsets = advanceOffsets.ToArray();
        var matrix = Matrix.Translate(0, walker.Rise) * walker.TextMatrix * walker.Ctm;
        runs.Add(new PositionedTextRun(text, operatorIndex, Quad(matrix, 0, text.Length, offsets, walker.FontSize), offsets,
            font, walker.FontSize, walker.HorizontalScale, walker.CharSpacing, walker.WordSpacing, matrix));
    }

    internal static TextQuadrilateral Quad(Matrix matrix, int offset, int length, IReadOnlyList<double> advanceOffsets, double fontSize)
    {
        var start = advanceOffsets[offset];
        var end = advanceOffsets[offset + length];
        var lowerLeft = matrix.Transform(start, 0);
        var lowerRight = matrix.Transform(end, 0);
        var upperRight = matrix.Transform(end, fontSize);
        var upperLeft = matrix.Transform(start, fontSize);
        return new TextQuadrilateral(
            new TextPoint(lowerLeft.X, lowerLeft.Y),
            new TextPoint(lowerRight.X, lowerRight.Y),
            new TextPoint(upperRight.X, upperRight.Y),
            new TextPoint(upperLeft.X, upperLeft.Y));
    }

    private static void Sort(List<PositionedTextRun> runs)
    {
        runs.Sort(static (a, b) =>
        {
            var aOrigin = a.Matrix.Transform(0, 0);
            var bOrigin = b.Matrix.Transform(0, 0);
            if (Math.Abs(aOrigin.Y - bOrigin.Y) > LineTolerance)
            {
                return bOrigin.Y.CompareTo(aOrigin.Y);
            }

            return aOrigin.X.CompareTo(bOrigin.X);
        });
    }

    private static ComposedText Compose(IReadOnlyList<PositionedTextRun> runs)
    {
        var text = new StringBuilder();
        var characters = new List<CharacterSource>();
        PositionedTextRun? previous = null;
        for (var runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            var run = runs[runIndex];
            if (previous is not null)
            {
                var previousOrigin = previous.Matrix.Transform(0, 0);
                var origin = run.Matrix.Transform(0, 0);
                if (Math.Abs(origin.Y - previousOrigin.Y) > LineTolerance)
                {
                    AddCharacter(text, characters, '\n', -1, -1);
                }
                else if (NeedsSpace(previous, run))
                {
                    AddCharacter(text, characters, ' ', -1, -1);
                }
            }

            for (var characterIndex = 0; characterIndex < run.Text.Length; characterIndex++)
            {
                AddCharacter(text, characters, run.Text[characterIndex], runIndex, characterIndex);
            }

            previous = run;
        }

        return new ComposedText(text.ToString(), characters);
    }

    private static bool NeedsSpace(PositionedTextRun previous, PositionedTextRun current)
    {
        if (previous.Text.Length == 0 || current.Text.Length == 0
            || char.IsWhiteSpace(previous.Text[^1]) || char.IsWhiteSpace(current.Text[0]))
        {
            return false;
        }

        var previousOrigin = previous.Matrix.Transform(0, 0);
        var currentOrigin = current.Matrix.Transform(0, 0);
        var previousPen = previous.Matrix.Transform(previous.Advance, 0);
        var emPoint = current.Matrix.Transform(current.FontSize, 0);
        var gap = currentOrigin.X - previousPen.X;
        var em = Math.Abs(emPoint.X - currentOrigin.X);
        return gap > SpaceGapEm * em;
    }

    private static void AddCharacter(StringBuilder text, List<CharacterSource> characters, char value, int runIndex, int characterIndex)
    {
        characters.Add(new CharacterSource(text.Length, runIndex, characterIndex));
        text.Append(value);
    }

    private static ComposedText Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        var characters = new List<CharacterSource>(text.Length);
        var inWhitespace = false;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                if (!inWhitespace)
                {
                    characters.Add(new CharacterSource(i, -1, -1));
                    builder.Append(' ');
                    inWhitespace = true;
                }
            }
            else
            {
                characters.Add(new CharacterSource(i, -1, -1));
                builder.Append(text[i]);
                inWhitespace = false;
            }
        }

        return new ComposedText(builder.ToString(), characters);
    }

    private static ComposedText Normalize(ComposedText value)
    {
        var builder = new StringBuilder(value.Text.Length);
        var characters = new List<CharacterSource>(value.Text.Length);
        var inWhitespace = false;
        for (var i = 0; i < value.Text.Length; i++)
        {
            if (char.IsWhiteSpace(value.Text[i]))
            {
                if (!inWhitespace)
                {
                    characters.Add(value.Characters[i]);
                    builder.Append(' ');
                    inWhitespace = true;
                }
            }
            else
            {
                characters.Add(value.Characters[i]);
                builder.Append(value.Text[i]);
                inWhitespace = false;
            }
        }

        return new ComposedText(builder.ToString(), characters);
    }

    private static bool IsWholeWord(string text, int index, int length)
        => (index == 0 || !IsWordCharacter(text[index - 1]))
        && (index + length == text.Length || !IsWordCharacter(text[index + length]));

    private static bool IsWordCharacter(char value) => char.IsLetterOrDigit(value) || value == '_';

    private static TextHit CreateHit(ComposedText original, ComposedText searchable, IReadOnlyList<PositionedTextRun> runs, int index, int length, int pageIndex)
    {
        var selected = new List<CharacterSource>(length);
        for (var i = index; i < index + length; i++)
        {
            var originalIndex = searchable.Characters[i].OriginalIndex;
            selected.Add(original.Characters[originalIndex]);
        }

        var quadrilaterals = new List<TextQuadrilateral>();
        var sources = new List<TextSourceReference>();
        var syntheticGapBoundaries = new List<bool>();
        var sawSyntheticWhitespace = false;
        for (var i = 0; i < selected.Count;)
        {
            var character = selected[i];
            if (character.RunIndex < 0)
            {
                sawSyntheticWhitespace |= char.IsWhiteSpace(original.Text[character.OriginalIndex]);
                i++;
                continue;
            }

            var segmentLength = 1;
            while (i + segmentLength < selected.Count
                && selected[i + segmentLength].RunIndex == character.RunIndex
                && selected[i + segmentLength].CharacterIndex == character.CharacterIndex + segmentLength)
            {
                segmentLength++;
            }

            var run = runs[character.RunIndex];
            if (sources.Count > 0)
            {
                syntheticGapBoundaries.Add(sawSyntheticWhitespace);
            }

            quadrilaterals.Add(Quad(run.Matrix, character.CharacterIndex, segmentLength, run.AdvanceOffsets, run.FontSize));
            sources.Add(new TextSourceReference(run.OperatorIndex, character.CharacterIndex, segmentLength));
            sawSyntheticWhitespace = false;
            i += segmentLength;
        }

        var firstOriginal = searchable.Characters[index].OriginalIndex;
        var lastOriginal = searchable.Characters[index + length - 1].OriginalIndex;
        var matchedText = original.Text.Substring(firstOriginal, lastOriginal - firstOriginal + 1);
        return new TextHit(matchedText, pageIndex, quadrilaterals, sources, syntheticGapBoundaries);
    }

    private readonly record struct CharacterSource(int OriginalIndex, int RunIndex, int CharacterIndex);

    private sealed record ComposedText(string Text, IReadOnlyList<CharacterSource> Characters);
}
