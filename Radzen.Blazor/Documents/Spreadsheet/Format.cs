using Radzen.Blazor.Rendering;
using System;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

internal static class FormatColorExtensions
{
    public static string ToXLSXColor(this string color)
    {
        ArgumentNullException.ThrowIfNull(color);
        var parsed = RGB.Parse(color) ?? throw new ArgumentException($"Invalid color value: {color}", nameof(color));
        return $"FF{parsed.ToHex()}";
    }
}

/// <summary>
/// Represents the line style for a cell border.
/// </summary>
public enum BorderLineStyle
{
    /// <summary>No border.</summary>
    None,
    /// <summary>Thin border.</summary>
    Thin,
    /// <summary>Medium border.</summary>
    Medium,
    /// <summary>Thick border.</summary>
    Thick,
    /// <summary>Dashed border.</summary>
    Dashed,
    /// <summary>Dotted border.</summary>
    Dotted,
    /// <summary>Double border.</summary>
    Double
}

/// <summary>
/// Represents a border style with color and line style.
/// </summary>
public class BorderStyle
{
    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public string Color { get; set; } = "#000000";

    /// <summary>
    /// Gets or sets the border line style.
    /// </summary>
    public BorderLineStyle LineStyle { get; set; } = BorderLineStyle.Thin;

    /// <summary>
    /// Creates a copy of this border style.
    /// </summary>
    public BorderStyle Clone() => new() { Color = Color, LineStyle = LineStyle };

    internal void AppendCss(StringBuilder sb, string side)
    {
        if (LineStyle == BorderLineStyle.None)
        {
            return;
        }

        sb.Append("border-");
        sb.Append(side);
        sb.Append(": ");
        sb.Append(LineStyle switch
        {
            BorderLineStyle.Thin => "1px solid ",
            BorderLineStyle.Medium => "2px solid ",
            BorderLineStyle.Thick => "3px solid ",
            BorderLineStyle.Dashed => "1px dashed ",
            BorderLineStyle.Dotted => "1px dotted ",
            BorderLineStyle.Double => "3px double ",
            _ => "1px solid "
        });
        sb.Append(Color);
        sb.Append(';');
    }

    internal string ToXlsxStyle() => LineStyle switch
    {
        BorderLineStyle.Thin => "thin",
        BorderLineStyle.Medium => "medium",
        BorderLineStyle.Thick => "thick",
        BorderLineStyle.Dashed => "dashed",
        BorderLineStyle.Dotted => "dotted",
        BorderLineStyle.Double => "double",
        _ => "thin"
    };

    internal static BorderLineStyle FromXlsxStyle(string? style) => style switch
    {
        "thin" => BorderLineStyle.Thin,
        "medium" => BorderLineStyle.Medium,
        "thick" => BorderLineStyle.Thick,
        "dashed" => BorderLineStyle.Dashed,
        "dotted" => BorderLineStyle.Dotted,
        "double" => BorderLineStyle.Double,
        "hair" => BorderLineStyle.Dotted,
        "mediumDashed" => BorderLineStyle.Dashed,
        _ => BorderLineStyle.None
    };
}

/// <summary>
/// Represents a format that can be applied to cells in a spreadsheet.
/// </summary>
public class Format
{
    private string? color;
    private string? background;
    private string? numberFormat;
    private string? fontFamily;
    private double? fontSize;

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
    /// Gets or sets whether the text in the format should have a strikethrough.
    /// </summary>
    public bool Strikethrough { get; set; }

    /// <summary>
    /// Gets or sets whether text should wrap within the cell.
    /// </summary>
    public bool WrapText { get; set; }

    /// <summary>
    /// Gets or sets the text alignment in the format.
    /// </summary>
    public TextAlign TextAlign { get; set; } = TextAlign.Left;

    /// <summary>
    /// Gets or sets the vertical alignment in the format.
    /// </summary>
    public VerticalAlign VerticalAlign { get; set; } = VerticalAlign.Top;

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string? FontFamily
    {
        get => fontFamily;
        set
        {
            if (fontFamily != value)
            {
                fontFamily = value;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public double? FontSize
    {
        get => fontSize;
        set
        {
            if (fontSize != value)
            {
                fontSize = value;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets or sets the top border style.
    /// </summary>
    public BorderStyle? BorderTop { get; set; }

    /// <summary>
    /// Gets or sets the right border style.
    /// </summary>
    public BorderStyle? BorderRight { get; set; }

    /// <summary>
    /// Gets or sets the bottom border style.
    /// </summary>
    public BorderStyle? BorderBottom { get; set; }

    /// <summary>
    /// Gets or sets the left border style.
    /// </summary>
    public BorderStyle? BorderLeft { get; set; }

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
    /// Gets or sets the number format code (e.g. "#,##0.00", "0%", "yyyy-mm-dd").
    /// </summary>
    public string? NumberFormat
    {
        get => numberFormat;
        set
        {
            if (numberFormat != value)
            {
                numberFormat = value;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether all format properties are at their default values.
    /// </summary>
    public bool IsDefault =>
        Color == null &&
        BackgroundColor == null &&
        !Bold &&
        !Italic &&
        !Underline &&
        !Strikethrough &&
        !WrapText &&
        FontFamily == null &&
        FontSize == null &&
        BorderTop == null &&
        BorderRight == null &&
        BorderBottom == null &&
        BorderLeft == null &&
        TextAlign == TextAlign.Left &&
        VerticalAlign == VerticalAlign.Top &&
        string.IsNullOrEmpty(NumberFormat);

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
        ArgumentNullException.ThrowIfNull(sb);
        if (Color != null)
        {
            sb.Append("color: ");
            sb.Append(Color);
            sb.Append(';');
        }

        if (BackgroundColor != null)
        {
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

        if (Underline && Strikethrough)
        {
            sb.Append("text-decoration: underline line-through;");
        }
        else if (Underline)
        {
            sb.Append("text-decoration: underline;");
        }
        else if (Strikethrough)
        {
            sb.Append("text-decoration: line-through;");
        }

        if (FontFamily != null)
        {
            sb.Append("font-family: ");
            sb.Append(FontFamily);
            sb.Append(';');
        }

        if (FontSize != null)
        {
            sb.Append("font-size: ");
            sb.Append(FontSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append("pt;");
        }

        if (WrapText)
        {
            sb.Append("white-space: pre-wrap; word-wrap: break-word;");
        }
        else
        {
            sb.Append("white-space: nowrap; overflow: hidden;");
        }

        BorderTop?.AppendCss(sb, "top");
        BorderRight?.AppendCss(sb, "right");
        BorderBottom?.AppendCss(sb, "bottom");
        BorderLeft?.AppendCss(sb, "left");

        if (VerticalAlign != VerticalAlign.Top || TextAlign != TextAlign.Left)
        {
            sb.Append("display: flex;");

            // Handle vertical alignment with align-items
            if (VerticalAlign != VerticalAlign.Top)
            {
                sb.Append("align-items: ");
                sb.Append(VerticalAlign switch
                {
                    VerticalAlign.Top => "flex-start",
                    VerticalAlign.Middle => "center",
                    VerticalAlign.Bottom => "flex-end",
                    _ => "flex-start"
                });
                sb.Append(';');
            }

            // Handle horizontal alignment with justify-content
            if (TextAlign != TextAlign.Left)
            {
                sb.Append("justify-content: ");
                sb.Append(TextAlign switch
                {
                    TextAlign.Center => "center",
                    TextAlign.Right => "flex-end",
                    TextAlign.Justify => "space-between",
                    _ => "flex-start"
                });
                sb.Append(';');
            }
        }
    }

    /// <summary>
    /// Creates a new format by merging this format with an overlay format.
    /// </summary>
    /// <param name="format">The format whose values should override this format.</param>
    /// <returns>A new <see cref="Format"/> instance representing the merged result.</returns>
    public Format Merge(Format format)
    {
        ArgumentNullException.ThrowIfNull(format);
        var merged = Clone();

        if (format.Color != null)
        {
            merged.Color = format.Color;
        }

        if (format.BackgroundColor != null)
        {
            merged.BackgroundColor = format.BackgroundColor;
        }

        if (format.Bold)
        {
            merged.Bold = true;
        }

        if (format.Italic)
        {
            merged.Italic = true;
        }

        if (format.Underline)
        {
            merged.Underline = true;
        }

        if (format.Strikethrough)
        {
            merged.Strikethrough = true;
        }

        if (format.WrapText)
        {
            merged.WrapText = true;
        }

        if (format.TextAlign != TextAlign.Left)
        {
            merged.TextAlign = format.TextAlign;
        }

        if (format.VerticalAlign != VerticalAlign.Top)
        {
            merged.VerticalAlign = format.VerticalAlign;
        }

        if (format.NumberFormat != null)
        {
            merged.NumberFormat = format.NumberFormat;
        }

        if (format.FontFamily != null)
        {
            merged.FontFamily = format.FontFamily;
        }

        if (format.FontSize != null)
        {
            merged.FontSize = format.FontSize;
        }

        if (format.BorderTop != null)
        {
            merged.BorderTop = format.BorderTop.Clone();
        }

        if (format.BorderRight != null)
        {
            merged.BorderRight = format.BorderRight.Clone();
        }

        if (format.BorderBottom != null)
        {
            merged.BorderBottom = format.BorderBottom.Clone();
        }

        if (format.BorderLeft != null)
        {
            merged.BorderLeft = format.BorderLeft.Clone();
        }

        return merged;
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
    /// This method is used to create a copy of the current format with a new text alignment setting.
    /// </summary>
    public Format WithTextAlign(TextAlign textAlign)
    {
        var clone = Clone();
        clone.TextAlign = textAlign;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new vertical alignment setting.
    /// </summary>
    public Format WithVerticalAlign(VerticalAlign verticalAlign)
    {
        var clone = Clone();
        clone.VerticalAlign = verticalAlign;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new number format code.
    /// </summary>
    public Format WithNumberFormat(string? numberFormat)
    {
        var clone = Clone();
        clone.NumberFormat = numberFormat;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new strikethrough setting.
    /// </summary>
    public Format WithStrikethrough(bool strikethrough)
    {
        var clone = Clone();
        clone.Strikethrough = strikethrough;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new font family.
    /// </summary>
    public Format WithFontFamily(string? fontFamily)
    {
        var clone = Clone();
        clone.FontFamily = fontFamily;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new font size.
    /// </summary>
    public Format WithFontSize(double? fontSize)
    {
        var clone = Clone();
        clone.FontSize = fontSize;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with a new wrap text setting.
    /// </summary>
    public Format WithWrapText(bool wrapText)
    {
        var clone = Clone();
        clone.WrapText = wrapText;
        return clone;
    }

    /// <summary>
    /// This method is used to create a copy of the current format with new border styles.
    /// </summary>
    public Format WithBorders(BorderStyle? top, BorderStyle? right, BorderStyle? bottom, BorderStyle? left)
    {
        var clone = Clone();
        clone.BorderTop = top?.Clone();
        clone.BorderRight = right?.Clone();
        clone.BorderBottom = bottom?.Clone();
        clone.BorderLeft = left?.Clone();
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
            Underline = Underline,
            Strikethrough = Strikethrough,
            WrapText = WrapText,
            TextAlign = TextAlign,
            VerticalAlign = VerticalAlign,
            NumberFormat = NumberFormat,
            FontFamily = FontFamily,
            FontSize = FontSize,
            BorderTop = BorderTop?.Clone(),
            BorderRight = BorderRight?.Clone(),
            BorderBottom = BorderBottom?.Clone(),
            BorderLeft = BorderLeft?.Clone()
        };
    }
}