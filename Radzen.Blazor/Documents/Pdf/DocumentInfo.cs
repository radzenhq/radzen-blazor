namespace Radzen.Documents.Pdf;


/// <summary>
/// Document metadata such as title, author and keywords.
/// </summary>
public class DocumentInfo
{
    /// <summary>Gets or sets the document title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the document author.</summary>
    public string? Author { get; set; }

    /// <summary>Gets or sets the document subject.</summary>
    public string? Subject { get; set; }

    /// <summary>Gets or sets the document keywords.</summary>
    public string? Keywords { get; set; }

    /// <summary>Gets or sets the name of the application that created the document.</summary>
    public string? Creator { get; set; }

    /// <summary>
    /// Gets or sets the name of the application that produced the PDF, written to the
    /// <c>/Info /Producer</c> entry and the XMP <c>pdf:Producer</c> property. When
    /// <see langword="null"/> the default producer is used and no producer is added to
    /// the <c>/Info</c> dictionary.
    /// </summary>
    public string? Producer { get; set; }

    /// <summary>
    /// Gets or sets the date the document was created, written to the <c>/Info
    /// /CreationDate</c> entry and the XMP <c>xmp:CreateDate</c> property. The caller
    /// supplies the value; no system clock is read. When <see langword="null"/> no
    /// creation date is written.
    /// </summary>
    public System.DateTimeOffset? CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the date the document was last modified, written to the <c>/Info
    /// /ModDate</c> entry and the XMP <c>xmp:ModifyDate</c> property. The caller supplies
    /// the value; no system clock is read. When <see langword="null"/> no modification
    /// date is written.
    /// </summary>
    public System.DateTimeOffset? ModificationDate { get; set; }
}
