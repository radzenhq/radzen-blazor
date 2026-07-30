using System.Collections.Immutable;
using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Geometry;

internal readonly record struct FontPaint(
    string Family,
    double Size,
    bool Bold,
    bool Italic,
    bool Underline,
    bool Strikethrough,
    Color Color);

internal readonly record struct InlineImagePaint
{
    public required SourceId Key { get; init; }

    public required SceneImageData Data { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}

internal readonly record struct CapturedSfntGlyph(
    ushort GlyphId,
    double Advance,
    double OffsetX,
    double OffsetY,
    double TextAdjustment,
    int Cluster,
    int Codepoint);

internal readonly record struct CapturedBuiltInGlyph(
    double Advance,
    double TextAdjustment,
    int Cluster,
    int Codepoint);

internal readonly record struct CapturedGlyphSpan(
    SfntFont? Face,
    ImmutableArray<CapturedSfntGlyph> SfntGlyphs,
    ImmutableArray<CapturedBuiltInGlyph> BuiltInGlyphs,
    double Advance,
    double XOffset)
{
    public bool IsSfnt => Face is not null;

    public int GlyphCount => IsSfnt ? SfntGlyphs.Length : BuiltInGlyphs.Length;

    public int WordSpaceCount
    {
        get
        {
            var count = 0;
            if (IsSfnt)
            {
                foreach (var glyph in SfntGlyphs)
                {
                    if (glyph.Codepoint == ' ')
                    {
                        count++;
                    }
                }
            }
            else
            {
                foreach (var glyph in BuiltInGlyphs)
                {
                    if (glyph.Codepoint == ' ')
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

}

internal readonly record struct CapturedGlyphRun(
    string Text,
    ImmutableArray<CapturedGlyphSpan> Spans,
    double Advance)
{
    public static CapturedGlyphRun Empty(string text) => new(text, [], 0);
}

internal readonly record struct FragmentPaint
{
    public required FontPaint Font { get; init; }

    public required double Opacity { get; init; }

    public required double LetterSpacing { get; init; }

    public required double WordSpacing { get; init; }

    public required double HorizontalScale { get; init; }

    public required double ScriptScale { get; init; }

    public required double Rise { get; init; }

    public required bool IsScript { get; init; }

    public required bool Invisible { get; init; }

    public InlineImagePaint? InlineImage { get; init; }

    public string? Link { get; init; }

    public string? LinkToAnchor { get; init; }

    public string? Anchor { get; init; }

    public required string RunText { get; init; }

    public string? LinkTarget => Link is { Length: > 0 } link ? link : null;

    public string? AnchorTarget
        => LinkTarget is null && LinkToAnchor is { Length: > 0 } anchor ? anchor : null;

}

internal struct LineFragment
{
    public required SourceId Source { get; init; }

    public required FragmentPaint Paint { get; init; }

    public SfntFont? Face { get; init; }

    public required string Text { get; init; }

    public required int Start { get; init; }

    public required int Length { get; init; }

    public double XOffset { get; init; }

    public required double Advance { get; init; }

    public bool IsMarker { get; init; }

    public CapturedGlyphRun GlyphRun { get; init; }

    public CapturedGlyphRun? CoalescedGlyphRun { get; init; }

    public bool SuppressTextEmission { get; init; }
}

internal sealed record LineBox
{
    public required ImmutableArray<LineFragment> Fragments { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }

    public double Baseline { get; init; }
}
