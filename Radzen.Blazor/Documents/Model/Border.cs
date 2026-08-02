using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A single border edge of a <see cref="Borders"/> box.
/// </summary>
public sealed class Border
{
    private Unit width;
    private Color color = Color.Black;
    private BorderStyle style = BorderStyle.None;

    internal Border()
    {
    }

    /// <summary>Gets or sets the border width. Defaults to zero.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit Width
    {
        get => width;
        set
        {
            width = AuthoredNumber.Absolute(value, "Border.Width");
            IsSet = true;
        }
    }

    /// <summary>Gets or sets the border color. Defaults to black.</summary>
    public Color Color
    {
        get => color;
        set
        {
            color = value;
            IsSet = true;
        }
    }

    /// <summary>Gets or sets the border line style. Defaults to <see cref="BorderStyle.None"/>.</summary>
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
}
