namespace Radzen.Documents;


/// <summary>
/// The page margins on each edge.
/// </summary>
public sealed class Margins
{
    /// <summary>Gets or sets the top margin.</summary>
    public Unit Top { get; set; }

    /// <summary>Gets or sets the right margin.</summary>
    public Unit Right { get; set; }

    /// <summary>Gets or sets the bottom margin.</summary>
    public Unit Bottom { get; set; }

    /// <summary>Gets or sets the left margin.</summary>
    public Unit Left { get; set; }

    /// <summary>
    /// Sets every edge to the same value.
    /// </summary>
    /// <param name="value">The margin applied to all four edges.</param>
    public void SetAll(Unit value)
    {
        Top = value;
        Right = value;
        Bottom = value;
        Left = value;
    }
}
