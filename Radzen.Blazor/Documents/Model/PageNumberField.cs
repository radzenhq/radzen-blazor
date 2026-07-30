namespace Radzen.Documents;


/// <summary>
/// An inline field that renders the number of the page it is emitted on. Resolved
/// per page wherever it is placed, including body content. Assigning
/// <see cref="Run.Text"/> has no effect - layout resolves the text per page.
/// </summary>
public sealed class PageNumberField : Run
{
    /// <summary>Initializes a new <see cref="PageNumberField"/>.</summary>
    public PageNumberField()
        : base("0")
    {
    }
}
