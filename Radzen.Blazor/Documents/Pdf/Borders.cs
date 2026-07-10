namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// The four border edges of a box. Box-level <see cref="Width"/>, <see cref="Color"/> and <see cref="Style"/>
/// flow to any edge that has not been individually set; an edge set explicitly keeps its own value.
/// </summary>
public class Borders
{
    /// <summary>
    /// Initializes a new <see cref="Borders"/> with edges that inherit the box-level values.
    /// </summary>
    public Borders()
    {
        Top = new Border(this);
        Right = new Border(this);
        Bottom = new Border(this);
        Left = new Border(this);
    }

    /// <summary>Gets or sets the box-level border width in points.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the box-level border color.</summary>
    public Color Color { get; set; } = Colors.Black;

    /// <summary>Gets or sets the box-level border line style.</summary>
    public BorderStyle Style { get; set; } = BorderStyle.None;

    /// <summary>Gets the top border edge.</summary>
    public Border Top { get; }

    /// <summary>Gets the right border edge.</summary>
    public Border Right { get; }

    /// <summary>Gets the bottom border edge.</summary>
    public Border Bottom { get; }

    /// <summary>Gets the left border edge.</summary>
    public Border Left { get; }
}
