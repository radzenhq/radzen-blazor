namespace Radzen.Documents;


/// <summary>
/// The four border edges of a box.
/// </summary>
public sealed class Borders
{
    /// <summary>Gets the top border edge.</summary>
    public Border Top { get; } = new();

    /// <summary>Gets the right border edge.</summary>
    public Border Right { get; } = new();

    /// <summary>Gets the bottom border edge.</summary>
    public Border Bottom { get; } = new();

    /// <summary>Gets the left border edge.</summary>
    public Border Left { get; } = new();
}
