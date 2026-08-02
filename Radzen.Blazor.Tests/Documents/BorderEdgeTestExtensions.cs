#nullable enable
using Radzen.Documents.Core;

namespace Radzen.Documents;

internal static class BorderEdgeTestExtensions
{
    public static Borders SetAll(
        this Borders borders,
        Unit? width = null,
        Color? color = null,
        BorderStyle? style = null)
    {
        foreach (var edge in new[] { borders.Top, borders.Right, borders.Bottom, borders.Left })
        {
            if (width is { } edgeWidth)
            {
                edge.Width = edgeWidth;
            }

            if (color is { } edgeColor)
            {
                edge.Color = edgeColor;
            }

            if (style is { } edgeStyle)
            {
                edge.Style = edgeStyle;
            }
        }

        return borders;
    }
}
