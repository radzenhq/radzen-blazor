namespace Radzen.Documents.Pdf;


/// <summary>
/// An inline field that renders the total number of pages in the document. Resolved
/// per page when placed in a section header or footer.
/// </summary>
public sealed class PageCountField : Run
{
    /// <summary>Initializes a new <see cref="PageCountField"/>.</summary>
    public PageCountField()
        : base("0")
    {
    }
}
