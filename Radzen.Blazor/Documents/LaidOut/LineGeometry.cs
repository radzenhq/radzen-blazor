using System.Collections.Immutable;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.LaidOut;

internal sealed record InlineImagePaint
{
    public required SourceId Key { get; init; }

    public required SceneImageData Data { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}

internal enum FormFieldKind
{
    Text,
    CheckBox,
    Radio,
    DropDown,
}

internal sealed record FormFieldPaint
{
    public required SourceId Key { get; init; }

    public required FormFieldKind Kind { get; init; }

    public required string Name { get; init; }

    public required string Value { get; init; }

    public required bool Required { get; init; }

    public required bool Chosen { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public required double Ascent { get; init; }

    public string? Label { get; init; }

    public ImmutableArray<string> Options { get; init; }

    public CapturedGlyphRun ValueGlyphs { get; init; }
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

    public FormFieldPaint? FormField { get; init; }

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

    public required string Text { get; init; }

    public required int Start { get; init; }

    public required int Length { get; init; }

    public double XOffset { get; init; }

    public required double Advance { get; init; }

    public bool IsMarker { get; init; }

    public required CapturedGlyphRun GlyphRun { get; init; }
}

internal readonly struct ShapedTextRun
{
    public required ImmutableArray<LineFragment> Fragments { get; init; }

    public required FragmentPaint Paint { get; init; }

    public required double XOffset { get; init; }

    public required bool IsMarker { get; init; }

    public required CapturedGlyphRun GlyphRun { get; init; }

    public SourceId Source => Fragments[0].Source;
}

internal sealed record LineBox
{
    public LineBox(ImmutableArray<ShapedTextRun> shapedRuns)
    {
        ShapedRuns = shapedRuns;
        if (shapedRuns.Length == 0)
        {
            Fragments = [];
            return;
        }

        if (shapedRuns.Length == 1)
        {
            Fragments = shapedRuns[0].Fragments;
            return;
        }

        var count = 0;
        foreach (var run in shapedRuns)
        {
            count += run.Fragments.Length;
        }

        var fragments = ImmutableArray.CreateBuilder<LineFragment>(count);
        foreach (var run in shapedRuns)
        {
            fragments.AddRange(run.Fragments);
        }

        Fragments = fragments.MoveToImmutable();
    }

    public ImmutableArray<ShapedTextRun> ShapedRuns { get; }

    public ImmutableArray<LineFragment> Fragments { get; }

    public double Width { get; init; }

    public double Height { get; init; }

    public double Baseline { get; init; }
}
