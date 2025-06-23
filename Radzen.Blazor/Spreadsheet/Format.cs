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
    private string? background;

    /// <summary>
    /// Gets or sets whether the text in the format should be bold.
    /// </summary>
    public bool Bold { get; set; }

    /// <summary>
    /// Gets or sets whether the text in the format should be italicized.
    /// </summary>
    public bool Italic { get; set; }

    /// <summary>
    /// Gets or sets whether the text in the format should be underlined.
    /// </summary>
    public bool Underline { get; set; }

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
        if (Bold)
        {
            sb.Append("font-weight: bold;");
        }
        if (Italic)
        {
            sb.Append("font-style: italic;");
        }
        if (Underline)
        {
            sb.Append("text-decoration: underline;");
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
    /// This method is used to create a copy of the current format with a new bold setting.
    /// </summary>
    public Format WithBold(bool bold)
    {
        var clone = Clone();
        clone.Bold = bold;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new italic setting.
    /// </summary>
    public Format WithItalic(bool italic)
    {
        var clone = Clone();
        clone.Italic = italic;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new underline setting.
    /// </summary>
    public Format WithUnderline(bool underline)
    {
        var clone = Clone();
        clone.Underline = underline;
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
            BackgroundColor = BackgroundColor,
            Bold = Bold,
            Italic = Italic,
            Underline = Underline
        };
    }
}