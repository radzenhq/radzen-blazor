namespace Radzen.Documents;


/// <summary>
/// A single border edge. When a property has not been set on the edge, it falls back to the
/// box-level value of its owning <see cref="Borders"/>.
/// </summary>
public sealed class Border
{
    private readonly Borders owner;
    private Unit? width;
    private Color? color;
    private BorderStyle? style;

    internal Border(Borders owner) => this.owner = owner;

    /// <summary>
    /// Gets or sets the border width. Falls back to the box width when not set on the edge.
    /// </summary>
    public Unit Width
    {
        get => width ?? owner.Width;
        set => width = value;
    }

    /// <summary>
    /// Gets or sets the border color. Falls back to the box color when not set on the edge.
    /// </summary>
    public Color Color
    {
        get => color ?? owner.Color;
        set => color = value;
    }

    /// <summary>
    /// Gets or sets the border line style. Falls back to the box style when not set on the edge.
    /// </summary>
    public BorderStyle Style
    {
        get => style ?? owner.Style;
        set => style = value;
    }

    internal bool IsSet => width is not null || color is not null || style is not null || owner.IsSet;
}
