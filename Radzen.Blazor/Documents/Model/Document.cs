using Radzen.Documents.Fonts;

namespace Radzen.Documents;

/// <summary>
/// The root of the document authoring model. Holds metadata, named styles, the ordered
/// sections and the fonts they resolve against. The model carries no output-format
/// concepts; render it with a format-specific renderer.
/// </summary>
public sealed class Document
{
    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the named style definitions.</summary>
    public StyleCollection Styles { get; } = [];

    /// <summary>Gets the ordered sections of the document.</summary>
    public SectionCollection Sections { get; } = new();

    /// <summary>Gets the font collection used to register and resolve fonts.</summary>
    public FontCollection Fonts { get; } = new();

    /// <summary>
    /// Gets or sets the natural language of the document as an RFC 3066 /
    /// BCP 47 tag (e.g. <c>en-US</c>). Required when the renderer is asked for
    /// accessible output: accessibility standards require the document language to be
    /// determinable, so renderers targeting them need it set.
    /// </summary>
    /// <exception cref="System.ArgumentException">The value is not a well-formed BCP 47 tag.</exception>
    public string? Language
    {
        get => language;
        set => language = Core.LanguageTag.Validated(value, "Document.Language");
    }

    private string? language;

    /// <summary>
    /// Takes a snapshot of the effective formatting of this document, resolving its named style
    /// chains once. Resolve many paragraphs or runs through the returned snapshot instead of
    /// calling the single-element overloads in a loop.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">A named style in the chain is undefined or cyclic.</exception>
    public DocumentFormatting Resolve() => new(StyleResolver.Resolve(this));

    /// <summary>
    /// Resolves the effective formatting of a paragraph in this document. Every value is resolved with the
    /// precedence described on <see cref="Style"/>: the value set directly on the paragraph wins, then the
    /// nearest named style in its chain that sets the value, then the built-in default.
    /// </summary>
    /// <param name="paragraph">A paragraph reachable from one of this document's sections.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="paragraph"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="paragraph"/> is not part of this document.</exception>
    /// <exception cref="System.InvalidOperationException">A named style in the chain is undefined or cyclic.</exception>
    public ParagraphFormat Resolve(Paragraph paragraph) => Resolve().Resolve(paragraph);

    /// <summary>
    /// Resolves the effective font values of a run in this document, cascading the run's own font over the
    /// containing paragraph, its named style chain, the enclosing elements and the built-in default.
    /// </summary>
    /// <param name="run">A run reachable from one of this document's sections.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="run"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="run"/> is not part of this document.</exception>
    /// <exception cref="System.InvalidOperationException">A named style in the chain is undefined or cyclic.</exception>
    public FontValues Resolve(TextInline run) => Resolve().Resolve(run);
}
