namespace Radzen.Documents.Layout;

internal readonly record struct ResolvedParagraphFormat
{
    public required HorizontalAlignment Alignment { get; init; }

    public required Unit SpacingBefore { get; init; }

    public required Unit SpacingAfter { get; init; }

    public required Unit LeftIndent { get; init; }

    public required bool KeepTogether { get; init; }

    public required bool KeepWithNext { get; init; }
}
