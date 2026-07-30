namespace Radzen.Documents;


/// <summary>
/// An inline field that renders the number of the page it is emitted on. Resolved
/// per page wherever it is placed, including body content.
/// </summary>
public sealed class PageNumberField : TextInline
{
    /// <summary>Initializes a new <see cref="PageNumberField"/>.</summary>
    public PageNumberField()
    {
    }

    internal override string LayoutText => "0";
}
