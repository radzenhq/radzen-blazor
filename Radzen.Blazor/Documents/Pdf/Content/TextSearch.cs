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
    public PdfRect Bounds => TextSearch.GetBounds([LowerLeft, LowerRight, UpperRight, UpperLeft]);
}

internal readonly struct TextSourceReference(int operatorIndex, int characterOffset, int characterLength)
{
    public int OperatorIndex { get; } = operatorIndex;

    public int CharacterOffset { get; } = characterOffset;

    public int CharacterLength { get; } = characterLength;
}

/// <summary>Represents decoded text and geometry from one text-show operator.</summary>
public sealed class PositionedTextRun
{
    internal PositionedTextRun(string text, int operatorIndex, TextQuadrilateral quadrilateral, double[] advanceOffsets,
        bool geometryEstimated, ReverseFont font, double fontSize, double scale, double charSpacing, double wordSpacing, Matrix matrix)
    {
        Text = text;
        OperatorIndex = operatorIndex;
        Quadrilateral = quadrilateral;
        AdvanceOffsets = advanceOffsets;
        GeometryEstimated = geometryEstimated;
        Font = font;
        FontSize = fontSize;
        Scale = scale;
        CharSpacing = charSpacing;
        WordSpacing = wordSpacing;
        Matrix = matrix;
    }

    /// <summary>Gets the decoded text.</summary>
    public string Text { get; }

    internal int OperatorIndex { get; }

    /// <summary>Gets the transformed em-box quadrilateral.</summary>
    public TextQuadrilateral Quadrilateral { get; }

    /// <summary>Gets the axis-aligned bounds enclosing the quadrilateral.</summary>
    public PdfRect Bounds => Quadrilateral.Bounds;

    /// <summary>
    /// Gets whether the run's font left at least one shown glyph without a usable width, so
    /// every advance from that glyph onwards, and the geometry derived from it, is estimated.
    /// </summary>
    public bool GeometryEstimated { get; }

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
        IReadOnlyList<bool> syntheticGapBoundaries, bool geometryEstimated)
    {
        Text = text;
        PageIndex = pageIndex;
        Quadrilaterals = quadrilaterals;
        Sources = sources;
        SyntheticGapBoundaries = syntheticGapBoundaries;
        GeometryEstimated = geometryEstimated;
        Bounds = TextSearch.GetBounds(quadrilaterals);
    }

    /// <summary>Gets the text as it appears in the extracted page text.</summary>
    public string Text { get; }

    /// <summary>Gets the zero-based page index, or -1 when searching a page directly.</summary>
    public int PageIndex { get; }

    /// <summary>Gets one quadrilateral for each intersected text-show run.</summary>
    public IReadOnlyList<TextQuadrilateral> Quadrilaterals { get; }

    /// <summary>Gets the axis-aligned bounds enclosing all match quadrilaterals.</summary>
    public PdfRect Bounds { get; }

    internal IReadOnlyList<TextSourceReference> Sources { get; }

    /// <summary>
    /// Gets whether <see cref="Quadrilaterals"/> and <see cref="Bounds"/> rest on an estimated
    /// glyph width because the source font does not provide one for every glyph shown.
    /// </summary>
    public bool GeometryEstimated { get; }

    internal IReadOnlyList<bool> SyntheticGapBoundaries { get; }
}

internal static class TextSearch
{
    public static IReadOnlyList<PositionedTextRun> Extract(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts, ContentTokenizer.Cache? cache = null)
    {
        var runs = Parse(content, fonts, cache);
        Sort(runs);
        return runs;
    }

    public static string ExtractText(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts, ContentTokenizer.Cache? cache = null)
    {
        var runs = Parse(content, fonts, cache);
        Sort(runs);
        return Compose(runs).Text;
    }

    public static IReadOnlyList<TextHit> Find(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts, string text, TextSearchOptions? options, int pageIndex, ContentTokenizer.Cache? cache = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(text));
        }

        options ??= new TextSearchOptions();
        var runs = Parse(content, fonts, cache);
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

    public static PdfRect GetBounds(IReadOnlyList<TextQuadrilateral> quadrilaterals)
    {
        if (quadrilaterals.Count == 0)
        {
            return new PdfRect();
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

    public static PdfRect GetBounds(IReadOnlyList<TextPoint> points)
    {
        var bounds = new PdfRectBounds();
        foreach (var point in points)
        {
            bounds.Include(point.X, point.Y);
        }

        return bounds.ToRect();
    }

    private static List<PositionedTextRun> Parse(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts, ContentTokenizer.Cache? cache)
    {
        if (content is null || content.Length == 0)
        {
            return [];
        }

        var runs = new List<PositionedTextRun>();
        ContentTextWalker.Walk(content, fonts, (walker, op, operands, array, operatorIndex) => op == "TJ"
            ? ShowArray(runs, array, walker, operatorIndex)
            : Show(runs, operands, walker, operatorIndex), cache);
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
        var estimated = AppendText(builder, advanceOffsets, reverse, bytes, walker.FontSize, walker.HorizontalScale, walker.CharSpacing, walker.WordSpacing);
        AddRun(runs, builder.ToString(), walker, reverse, advanceOffsets, estimated, operatorIndex);
        return advanceOffsets[^1];
    }

    private static double ShowArray(List<PositionedTextRun> runs, List<Token> array, ContentTextWalker walker, int operatorIndex)
    {
        var reverse = walker.Font ?? ReverseFont.WinAnsi;
        var fontSize = walker.FontSize;
        var scale = walker.HorizontalScale;
        var builder = new StringBuilder();
        var advanceOffsets = new List<double> { 0.0 };
        var estimated = false;
        foreach (var element in array)
        {
            if (element.Kind == TokenKind.String && element.Bytes is { Length: > 0 } bytes)
            {
                estimated |= AppendText(builder, advanceOffsets, reverse, bytes, fontSize, scale, walker.CharSpacing, walker.WordSpacing);
            }
            else if (element.Kind == TokenKind.Number)
            {
                var advance = advanceOffsets[^1] - element.Number / 1000.0 * fontSize * scale;
                if (element.Number <= -TextComposition.TjSpaceThreshold)
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

        AddRun(runs, builder.ToString(), walker, reverse, advanceOffsets, estimated, operatorIndex);
        return advanceOffsets[^1];
    }

    private static bool AppendText(StringBuilder builder, List<double> advanceOffsets, ReverseFont font, byte[] bytes, double fontSize, double scale, double charSpacing, double wordSpacing)
    {
        var advance = advanceOffsets[^1];
        var estimated = false;
        foreach (var code in font.DecodeCodes(bytes))
        {
            var glyphAdvance = LoadedGlyphAdvance.Calculate(
                font, code.Code, code.IsWordSpace, fontSize, scale, charSpacing, wordSpacing,
                MissingWidthPolicy.Estimate, out var glyphEstimated);
            estimated |= glyphEstimated;
            for (var i = 0; i < code.Text.Length; i++)
            {
                builder.Append(code.Text[i]);
                if (i == code.Text.Length - 1)
                {
                    advance += glyphAdvance;
                }

                advanceOffsets.Add(advance);
            }
        }

        return estimated;
    }

    private static void AddRun(List<PositionedTextRun> runs, string text, ContentTextWalker walker, ReverseFont font,
        List<double> advanceOffsets, bool geometryEstimated, int operatorIndex)
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
        var matrix = Matrix.RawTranslate(0, walker.Rise) * walker.TextMatrix * walker.Ctm;
        runs.Add(new PositionedTextRun(text, operatorIndex, Quad(matrix, 0, text.Length, offsets, walker.FontSize), offsets,
            geometryEstimated, font, walker.FontSize, walker.HorizontalScale, walker.CharSpacing, walker.WordSpacing, matrix));
    }

    internal static TextQuadrilateral Quad(Matrix matrix, int offset, int length, IReadOnlyList<double> advanceOffsets, double fontSize)
        => Quad(matrix, offset, length, advanceOffsets, 0, fontSize);

    internal static TextQuadrilateral Quad(
        Matrix matrix, int offset, int length, IReadOnlyList<double> advanceOffsets, double bottom, double top)
    {
        var start = advanceOffsets[offset];
        var end = advanceOffsets[offset + length];
        var lowerLeft = matrix.Transform(start, bottom);
        var lowerRight = matrix.Transform(end, bottom);
        var upperRight = matrix.Transform(end, top);
        var upperLeft = matrix.Transform(start, top);
        return new TextQuadrilateral(
            new TextPoint(lowerLeft.X, lowerLeft.Y),
            new TextPoint(lowerRight.X, lowerRight.Y),
            new TextPoint(upperRight.X, upperRight.Y),
            new TextPoint(upperLeft.X, upperLeft.Y));
    }

    private static void Sort(List<PositionedTextRun> runs)
        => runs.Sort(static (a, b) => TextComposition.Compare(Place(a), Place(b)));

    private static TextComposition.Placement Place(PositionedTextRun run)
        => TextComposition.Place(run.Matrix, run.Advance, run.FontSize);

    private static ComposedText Compose(IReadOnlyList<PositionedTextRun> runs)
    {
        var text = new StringBuilder();
        var characters = new List<CharacterSource>();
        PositionedTextRun? previous = null;
        for (var runIndex = 0; runIndex < runs.Count; runIndex++)
        {
            var run = runs[runIndex];
            if (previous is not null
                && TextComposition.Separator(Place(previous), previous.Text, Place(run), run.Text) is { } separator)
            {
                AddCharacter(text, characters, separator, -1, -1);
            }

            for (var characterIndex = 0; characterIndex < run.Text.Length; characterIndex++)
            {
                AddCharacter(text, characters, run.Text[characterIndex], runIndex, characterIndex);
            }

            previous = run;
        }

        return new ComposedText(text.ToString(), characters);
    }

    private static void AddCharacter(StringBuilder text, List<CharacterSource> characters, char value, int runIndex, int characterIndex)
    {
        characters.Add(new CharacterSource(text.Length, runIndex, characterIndex));
        text.Append(value);
    }

    private static ComposedText Normalize(string text, IReadOnlyList<CharacterSource>? source = null)
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
                    characters.Add(source is null ? new CharacterSource(i, -1, -1) : source[i]);
                    builder.Append(' ');
                    inWhitespace = true;
                }
            }
            else
            {
                characters.Add(source is null ? new CharacterSource(i, -1, -1) : source[i]);
                builder.Append(text[i]);
                inWhitespace = false;
            }
        }

        return new ComposedText(builder.ToString(), characters);
    }

    private static ComposedText Normalize(ComposedText value) => Normalize(value.Text, value.Characters);

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
        var geometryEstimated = false;
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
            geometryEstimated |= run.GeometryEstimated;
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
        return new TextHit(matchedText, pageIndex, quadrilaterals, sources, syntheticGapBoundaries, geometryEstimated);
    }

    private readonly record struct CharacterSource(int OriginalIndex, int RunIndex, int CharacterIndex);

    private sealed record ComposedText(string Text, IReadOnlyList<CharacterSource> Characters);
}
