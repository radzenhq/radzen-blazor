namespace Radzen.Documents;


/// <summary>
/// The four border edges of a box. Box-level <see cref="Width"/>, <see cref="Color"/> and <see cref="Style"/>
/// flow to any edge that has not been individually set; an edge set explicitly keeps its own value.
/// </summary>
public sealed class Borders
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

    private Unit width;
    private Color color = Color.Black;
    private BorderStyle style = BorderStyle.None;

    /// <summary>Gets or sets the box-level border width.</summary>
    public Unit Width
    {
        get => width;
        set
        {
            width = value;
            IsSet = true;
        }
    }

    /// <summary>Gets or sets the box-level border color.</summary>
    public Color Color
    {
        get => color;
        set
        {
            color = value;
            IsSet = true;
        }
    }

    /// <summary>Gets or sets the box-level border line style.</summary>
    public BorderStyle Style
    {
        get => style;
        set
        {
            style = value;
            IsSet = true;
        }
    }

    internal bool IsSet { get; private set; }

    /// <summary>Gets the top border edge.</summary>
    public Border Top { get; }

    /// <summary>Gets the right border edge.</summary>
    public Border Right { get; }

    /// <summary>Gets the bottom border edge.</summary>
    public Border Bottom { get; }

    /// <summary>Gets the left border edge.</summary>
    public Border Left { get; }
}
