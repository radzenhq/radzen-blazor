namespace Radzen.Documents;


/// <summary>
/// An inline field that renders the total number of pages in the document. Resolved
/// per page wherever it is placed, including body content.
/// </summary>
public sealed class PageCountField : TextInline
{
    /// <summary>Initializes a new <see cref="PageCountField"/>.</summary>
    public PageCountField()
    {
    }

    internal override string LayoutText => PageFieldPlaceholder.Text;
}
