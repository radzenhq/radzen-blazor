using Radzen.Documents.Fonts;

namespace Radzen.Documents;

/// <summary>
/// A snapshot of the effective formatting of every paragraph and run in a
/// <see cref="Document"/>, taken by <see cref="Document.Resolve()"/>. Resolving many
/// elements through one snapshot walks the document once; the snapshot does not observe
/// later edits, so take a new one after changing the document.
/// </summary>
public sealed class DocumentFormatting
{
    private readonly StyleResolution styles;

    internal DocumentFormatting(StyleResolution styles) => this.styles = styles;

    /// <summary>
    /// Resolves the effective formatting of a paragraph. Every value is resolved with the
    /// precedence described on <see cref="Style"/>: the value set directly on the paragraph wins, then the
    /// nearest named style in its chain that sets the value, then the built-in default.
    /// </summary>
    /// <param name="paragraph">A paragraph reachable from one of the document's sections.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="paragraph"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="paragraph"/> is not part of the document.</exception>
    public ParagraphFormat Resolve(Paragraph paragraph)
    {
        System.ArgumentNullException.ThrowIfNull(paragraph);

        if (!styles.Contains(paragraph))
        {
            throw new System.ArgumentException(
                "The paragraph is not part of this document. Add it to a section, header or footer before resolving it.",
                nameof(paragraph));
        }

        var format = styles.Format(paragraph);

        return new ParagraphFormat
        {
            Alignment = format.Alignment,
            SpacingBefore = format.SpacingBefore,
            SpacingAfter = format.SpacingAfter,
            LeftIndent = format.LeftIndent,
            KeepTogether = format.KeepTogether,
            KeepWithNext = format.KeepWithNext,
            HeadingLevel = styles.HeadingLevel(paragraph) is var level and > 0 ? level : null,
            Font = (styles.ParagraphFont(paragraph) ?? paragraph.Font).Effective(),
        };
    }

    /// <summary>
    /// Resolves the effective font values of a run, cascading the run's own font over the
    /// containing paragraph, its named style chain, the enclosing elements and the built-in default.
    /// </summary>
    /// <param name="run">A run reachable from one of the document's sections.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="run"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="run"/> is not part of the document.</exception>
    public FontValues Resolve(TextInline run)
    {
        System.ArgumentNullException.ThrowIfNull(run);

        var font = styles.RunFont(run)
            ?? throw new System.ArgumentException(
                "The run is not part of this document. Add it to a paragraph in a section, header or footer before resolving it.",
                nameof(run));

        return font.Effective();
    }
}
