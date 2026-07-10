namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A single border edge. When a property has not been set on the edge, it falls back to the
/// box-level value of its owning <see cref="Borders"/>.
/// </summary>
public class Border
{
    private readonly Borders? owner;
    private double? width;
    private Color? color;
    private BorderStyle? style;

    /// <summary>
    /// Initializes a standalone border edge with no owning box.
    /// </summary>
    public Border()
    {
    }

    internal Border(Borders owner) => this.owner = owner;

    /// <summary>
    /// Gets or sets the border width in points. Falls back to the box width when not set on the edge.
    /// </summary>
    public double Width
    {
        get => width ?? owner?.Width ?? 0;
        set => width = value;
    }

    /// <summary>
    /// Gets or sets the border color. Falls back to the box color when not set on the edge.
    /// </summary>
    public Color Color
    {
        get => color ?? owner?.Color ?? Colors.Black;
        set => color = value;
    }

    /// <summary>
    /// Gets or sets the border line style. Falls back to the box style when not set on the edge.
    /// </summary>
    public BorderStyle Style
    {
        get => style ?? owner?.Style ?? BorderStyle.None;
        set => style = value;
    }
}
