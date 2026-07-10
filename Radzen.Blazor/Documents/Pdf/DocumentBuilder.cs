namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// The root of the document authoring model. Holds metadata, named styles and the ordered sections.
/// </summary>
public class DocumentBuilder
{
    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the named style definitions.</summary>
    public StyleCollection Styles { get; } = [];

    /// <summary>Gets the ordered sections of the document.</summary>
    public SectionCollection Sections { get; } = new();

    /// <summary>Gets the font collection used to register and resolve fonts.</summary>
    public FontCollection Fonts { get; } = new();
}
