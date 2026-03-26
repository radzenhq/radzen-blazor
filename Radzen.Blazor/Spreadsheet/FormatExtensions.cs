using System;
using System.Text;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

internal static class CellFormatExtensions
{
    internal static void ApplyFormat(this Cell cell, StringBuilder sb)
    {
        ArgumentNullException.ThrowIfNull(sb);
        cell.GetEffectiveFormat()?.AppendStyle(sb);
    }
}

internal static class FormatExtensions
{
    internal static void AppendStyle(this Format format, StringBuilder sb)
    {
        ArgumentNullException.ThrowIfNull(sb);
        if (format.Color is not null)
        {
            sb.Append("color: ");
            sb.Append(format.Color);
            sb.Append(';');
        }

        if (format.BackgroundColor is not null)
        {
            sb.Append("background-color: ");
            sb.Append(format.BackgroundColor);
            sb.Append(';');
        }

        if (format.Bold)
        {
            sb.Append("font-weight: bold;");
        }

        if (format.Italic)
        {
            sb.Append("font-style: italic;");
        }

        if (format.Underline && format.Strikethrough)
        {
            sb.Append("text-decoration: underline line-through;");
        }
        else if (format.Underline)
        {
            sb.Append("text-decoration: underline;");
        }
        else if (format.Strikethrough)
        {
            sb.Append("text-decoration: line-through;");
        }

        if (format.FontFamily is not null)
        {
            sb.Append("font-family: ");
            sb.Append(format.FontFamily);
            sb.Append(';');
        }

        if (format.FontSize is not null)
        {
            sb.Append("font-size: ");
            sb.Append(format.FontSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append("pt;");
        }

        if (format.WrapText)
        {
            sb.Append("white-space: pre-wrap; word-wrap: break-word;");
        }
        else
        {
            sb.Append("white-space: nowrap; overflow: hidden;");
        }

        format.BorderTop?.AppendCss(sb, "top");
        format.BorderRight?.AppendCss(sb, "right");
        format.BorderBottom?.AppendCss(sb, "bottom");
        format.BorderLeft?.AppendCss(sb, "left");

        if (format.VerticalAlign != VerticalAlign.Top || format.TextAlign != TextAlign.Left)
        {
            sb.Append("display: flex;");

            if (format.VerticalAlign != VerticalAlign.Top)
            {
                sb.Append("align-items: ");
                sb.Append(format.VerticalAlign switch
                {
                    VerticalAlign.Top => "flex-start",
                    VerticalAlign.Middle => "center",
                    VerticalAlign.Bottom => "flex-end",
                    _ => "flex-start"
                });
                sb.Append(';');
            }

            if (format.TextAlign != TextAlign.Left)
            {
                sb.Append("justify-content: ");
                sb.Append(format.TextAlign switch
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

    internal static void AppendFontStyle(this Format format, StringBuilder sb)
    {
        ArgumentNullException.ThrowIfNull(sb);

        if (format.Color is not null)
        {
            sb.Append("--rz-cell-color: ");
            sb.Append(format.Color);
            sb.Append(';');
        }

        if (format.BackgroundColor is not null)
        {
            sb.Append("background-color: ");
            sb.Append(format.BackgroundColor);
            sb.Append(';');
        }

        if (format.FontFamily is not null)
        {
            sb.Append("font-family: ");
            sb.Append(format.FontFamily);
            sb.Append(';');
        }

        if (format.FontSize is not null)
        {
            sb.Append("font-size: ");
            sb.Append(format.FontSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append("pt;");
        }
    }

    internal static void AppendCss(this BorderStyle border, StringBuilder sb, string side)
    {
        if (border.LineStyle == BorderLineStyle.None)
        {
            return;
        }

        sb.Append("border-");
        sb.Append(side);
        sb.Append(": ");
        sb.Append(border.LineStyle switch
        {
            BorderLineStyle.Thin => "1px solid ",
            BorderLineStyle.Medium => "2px solid ",
            BorderLineStyle.Thick => "3px solid ",
            BorderLineStyle.Dashed => "1px dashed ",
            BorderLineStyle.Dotted => "1px dotted ",
            BorderLineStyle.Double => "3px double ",
            _ => "1px solid "
        });
        sb.Append(border.Color);
        sb.Append(';');
    }
}
