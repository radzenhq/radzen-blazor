namespace Radzen.Documents.Pdf;


/// <summary>
/// An inline field that renders the number of the page it is emitted on. Resolved
/// per page when placed in a section header or footer.
/// </summary>
public sealed class PageNumberField : Run
{
    /// <summary>Initializes a new <see cref="PageNumberField"/>.</summary>
    public PageNumberField()
        : base("0")
    {
    }
}
