using System;

namespace Radzen.Documents.Pdf;


/// <summary>
/// Document metadata such as title, author and keywords.
/// </summary>
public class DocumentInfo
{
    private bool touched;
    private string? title;
    private string? author;
    private string? subject;
    private string? keywords;
    private string? creator;
    private string? producer;
    private DateTimeOffset? creationDate;
    private DateTimeOffset? modificationDate;

    /// <summary>Gets or sets the document title.</summary>
    public string? Title
    {
        get => title;
        set => Set(ref title, value);
    }

    /// <summary>Gets or sets the document author.</summary>
    public string? Author
    {
        get => author;
        set => Set(ref author, value);
    }

    /// <summary>Gets or sets the document subject.</summary>
    public string? Subject
    {
        get => subject;
        set => Set(ref subject, value);
    }

    /// <summary>Gets or sets the document keywords.</summary>
    public string? Keywords
    {
        get => keywords;
        set => Set(ref keywords, value);
    }

    /// <summary>Gets or sets the name of the application that created the document.</summary>
    public string? Creator
    {
        get => creator;
        set => Set(ref creator, value);
    }

    /// <summary>
    /// Gets or sets the name of the application that produced the PDF, written to the
    /// <c>/Info /Producer</c> entry and the XMP <c>pdf:Producer</c> property. When
    /// <see langword="null"/> the default producer is used and no producer is added to
    /// the <c>/Info</c> dictionary.
    /// </summary>
    public string? Producer
    {
        get => producer;
        set => Set(ref producer, value);
    }

    /// <summary>
    /// Gets or sets the date the document was created, written to the <c>/Info
    /// /CreationDate</c> entry and the XMP <c>xmp:CreateDate</c> property. The caller
    /// supplies the value; no system clock is read. When <see langword="null"/> no
    /// creation date is written.
    /// </summary>
    public DateTimeOffset? CreationDate
    {
        get => creationDate;
        set => Set(ref creationDate, value);
    }

    /// <summary>
    /// Gets or sets the date the document was last modified, written to the <c>/Info
    /// /ModDate</c> entry and the XMP <c>xmp:ModifyDate</c> property. The caller supplies
    /// the value; no system clock is read. When <see langword="null"/> no modification
    /// date is written.
    /// </summary>
    public DateTimeOffset? ModificationDate
    {
        get => modificationDate;
        set => Set(ref modificationDate, value);
    }

    /// <summary>
    /// Gets a value indicating whether a modeled metadata field has been assigned since the
    /// document was loaded. A loaded document emits an /Info override only when this is true.
    /// </summary>
    public bool IsModified => touched;

    // Unconditional, as on ContentElement: whether the new value is "equal" to the old is not
    // the same question as whether it emits the same bytes.
    private void Set<T>(ref T field, T value)
    {
        field = value;
        touched = true;
    }

    // Called once after load, which fills these same setters and would otherwise leave every
    // loaded document born dirty.
    internal void AcceptChanges() => touched = false;

    internal DocumentInfo Clone()
    {
        var copy = new DocumentInfo();
        CopyTo(copy);
        return copy;
    }

    internal void CopyTo(DocumentInfo target)
    {
        target.Title = Title;
        target.Author = Author;
        target.Subject = Subject;
        target.Keywords = Keywords;
        target.Creator = Creator;
        target.Producer = Producer;
        target.CreationDate = CreationDate;
        target.ModificationDate = ModificationDate;
    }
}
