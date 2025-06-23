using System;
using System.Text;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a format that can be applied to cells in a spreadsheet.
/// </summary>
public class Format
{
    private string? color;

    /// <summary>
    /// Gets or sets the color of the text in the format.
    /// </summary>
    public string? Color
    {
        get => color;
        set
        {
            if (color != value)
            {
                color = value;
                Changed?.Invoke();
            }
        }
    }

    private string? background;

    /// <summary>
    /// Gets or sets the background color of the format.
    /// </summary>
    public string? BackgroundColor
    {
        get => background;
        set
        {
            if (background != value)
            {
                background = value;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Occurs when the format is changed, allowing for updates to be made to the UI or other components that depend on this format.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Appends the CSS styles defined by this format to the provided StringBuilder.
    /// </summary>
    /// <param name="sb"></param>
    public void AppendStyle(StringBuilder sb)
    {
        if (Color != null)
        {
            sb.Append("color: ");
            sb.Append(Color);
            sb.Append(';');
        }

        if (BackgroundColor != null)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.Append("background-color: ");
            sb.Append(BackgroundColor);
            sb.Append(';');
        }
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new color.
    /// </summary>
    public Format WithColor(string? color)
    {
        var clone = Clone();

        clone.Color = color;

        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new background color.
    /// </summary>
    public Format WithBackgroundColor(string? backgroundColor)
    {
        var clone = Clone();
        clone.BackgroundColor = backgroundColor;
        return clone;
    }

    /// <summary>
    /// Creates a new instance of the Format class that is a copy of the current instance.
    /// </summary>
    public Format Clone()
    {
        return new Format
        {
            Color = Color,
            BackgroundColor = BackgroundColor
        };
    }
}