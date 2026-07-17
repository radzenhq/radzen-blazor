using System;

namespace Radzen.Documents.Pdf;


/// <summary>
/// Document metadata such as title, author and keywords.
/// </summary>
public class DocumentInfo : ITracksChanges
{
    private ChangeTracker tracker;
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
        set => tracker.Set(ref title, value);
    }

    /// <summary>Gets or sets the document author.</summary>
    public string? Author
    {
        get => author;
        set => tracker.Set(ref author, value);
    }

    /// <summary>Gets or sets the document subject.</summary>
    public string? Subject
    {
        get => subject;
        set => tracker.Set(ref subject, value);
    }

    /// <summary>Gets or sets the document keywords.</summary>
    public string? Keywords
    {
        get => keywords;
        set => tracker.Set(ref keywords, value);
    }

    /// <summary>Gets or sets the name of the application that created the document.</summary>
    public string? Creator
    {
        get => creator;
        set => tracker.Set(ref creator, value);
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
        set => tracker.Set(ref producer, value);
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
        set => tracker.Set(ref creationDate, value);
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
        set => tracker.Set(ref modificationDate, value);
    }

    /// <summary>
    /// Gets a value indicating whether a modeled metadata field has been assigned since the
    /// document was loaded. A loaded document emits an /Info override only when this is true.
    /// </summary>
    public bool IsModified => tracker.IsModified;

    internal void AcceptChanges() => tracker.AcceptChanges();

    void ITracksChanges.AcceptChanges() => AcceptChanges();

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
