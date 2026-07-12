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
}
