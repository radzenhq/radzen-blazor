namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a markdown inline code block: <c>`code`</c>.
/// </summary>
/// <param name="value"></param>
public class Code(string value) : Inline
{
    /// <summary>
    /// Gets or sets the code value.
    /// </summary>
    public string Value { get; set; } = value;

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitCode(this);
    }
}