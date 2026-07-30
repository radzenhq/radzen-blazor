namespace Radzen.Documents;


/// <summary>
/// An inline field that renders the total number of pages in the document. Resolved
/// per page wherever it is placed, including body content. Assigning
/// <see cref="Run.Text"/> has no effect - layout resolves the text per page.
/// </summary>
public sealed class PageCountField : Run
{
    /// <summary>Initializes a new <see cref="PageCountField"/>.</summary>
    public PageCountField()
        : base("0")
    {
    }
}
