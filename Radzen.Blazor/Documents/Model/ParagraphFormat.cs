using Radzen.Documents.Fonts;

namespace Radzen.Documents;

/// <summary>
/// The effective paragraph formatting values, with every value resolved through the direct local value,
/// the named style chain and the built-in default.
/// </summary>
public readonly record struct ParagraphFormat
{
    /// <summary>Gets the effective horizontal alignment.</summary>
    public required HorizontalAlignment Alignment { get; init; }

    /// <summary>Gets the effective spacing before the paragraph.</summary>
    public required Unit SpacingBefore { get; init; }

    /// <summary>Gets the effective spacing after the paragraph.</summary>
    public required Unit SpacingAfter { get; init; }

    /// <summary>Gets the effective left indent.</summary>
    public required Unit LeftIndent { get; init; }

    /// <summary>Gets the effective keep-together flag.</summary>
    public required bool KeepTogether { get; init; }

    /// <summary>Gets the effective keep-with-next flag.</summary>
    public required bool KeepWithNext { get; init; }

    /// <summary>
    /// Gets the effective heading level, from 1 to 6, or <see langword="null"/> when the paragraph
    /// is not a heading.
    /// </summary>
    public required int? HeadingLevel { get; init; }

    /// <summary>Gets the effective font values.</summary>
    public required FontValues Font { get; init; }
}
