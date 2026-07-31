using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.LaidOut;

internal readonly record struct InlineImagePaint
{
    public required SourceId Key { get; init; }

    public required SceneImageData Data { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
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

    public string? LinkTarget => Link is { Length: > 0 } link ? link : null;

    public string? AnchorTarget
        => LinkTarget is null && LinkToAnchor is { Length: > 0 } anchor ? anchor : null;

}

internal readonly struct LineFragment
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

    public required CapturedGlyphRun GlyphRun { get; init; }
}

internal readonly record struct ShapedRunSource(SourceId Source, int Start, int Length);

internal readonly struct ShapedTextRun
{
    public required int FirstFragment { get; init; }

    public required FragmentPaint Paint { get; init; }

    public required double XOffset { get; init; }

    public required bool IsMarker { get; init; }

    public required CapturedGlyphRun GlyphRun { get; init; }

    public required ImmutableArray<ShapedRunSource> Sources { get; init; }

    public SourceId Source => Sources[0].Source;
}

internal sealed record LineBox
{
    public required ImmutableArray<LineFragment> Fragments { get; init; }

    public ImmutableArray<ShapedTextRun> ShapedRuns { get; init; } = [];

    public double Width { get; init; }

    public double Height { get; init; }

    public double Baseline { get; init; }
}
