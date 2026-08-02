using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// The page margins on each edge.
/// </summary>
public sealed class Margins
{
    internal Margins()
    {
    }

    private Unit top;
    private Unit right;
    private Unit bottom;
    private Unit left;

    /// <summary>Gets or sets the top margin.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit Top
    {
        get => top;
        set => top = AuthoredNumber.Absolute(value, "Margins.Top");
    }

    /// <summary>Gets or sets the right margin.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit Right
    {
        get => right;
        set => right = AuthoredNumber.Absolute(value, "Margins.Right");
    }

    /// <summary>Gets or sets the bottom margin.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit Bottom
    {
        get => bottom;
        set => bottom = AuthoredNumber.Absolute(value, "Margins.Bottom");
    }

    /// <summary>Gets or sets the left margin.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit Left
    {
        get => left;
        set => left = AuthoredNumber.Absolute(value, "Margins.Left");
    }

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
