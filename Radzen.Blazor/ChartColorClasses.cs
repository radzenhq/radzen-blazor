namespace Radzen.Blazor
{
    static class ChartColorClasses
    {
        const int ShadeCount = 3;

        const int SankeyColorCount = 24;

        internal static int ColorCount(this ColorScheme scheme) => scheme switch
        {
            ColorScheme.Palette => 8,
            ColorScheme.Monochrome => 8,
            ColorScheme.Divergent => 9,
            _ => 24
        };

        internal static bool IsBeyond(this ColorScheme scheme, int index) => index >= scheme.ColorCount();

        internal static int ColorIndex(this ColorScheme scheme, int index) => index < 0 ? index : index % scheme.ColorCount();

        internal static int Shade(this ColorScheme scheme, int index) => (index / scheme.ColorCount() - 1) % ShadeCount + 1;

        internal static string? ColorClass(this ColorScheme scheme, int index) =>
            scheme.IsBeyond(index) ? $"rz-series-color-{scheme.ColorIndex(index)} rz-series-shade-{scheme.Shade(index)}" : null;

        internal static string SeriesClass(this ColorScheme scheme, int index) =>
            scheme.IsBeyond(index) ? $"rz-series-{index} {scheme.ColorClass(index)}" : $"rz-series-{index}";

        internal static string SeriesItemClass(this ColorScheme scheme, int index) =>
            scheme.IsBeyond(index) ? $"rz-series-item-{index} {scheme.ColorClass(index)}" : $"rz-series-item-{index}";

        internal static string SeriesTooltipClass(this ColorScheme scheme, int index) =>
            scheme.IsBeyond(index) ? $"rz-series-{index}-tooltip {scheme.TooltipColorClass(index)}" : $"rz-series-{index}-tooltip";

        internal static string SeriesItemTooltipClass(this ColorScheme scheme, int index) =>
            scheme.IsBeyond(index) ? $"rz-series-item-{index} {scheme.TooltipColorClass(index)}" : $"rz-series-item-{index}";

        internal static string SeriesColorVariable(this ColorScheme scheme, int index)
        {
            var variable = $"var(--rz-series-{scheme.ColorIndex(index) + 1})";

            return scheme.IsBeyond(index) ? $"color-mix(in oklch, {variable}, var(--rz-series-shade-color, white) var(--rz-series-shade, 0%))" : variable;
        }

        internal static string SankeyItemClass(int index) =>
            index < SankeyColorCount ? $"rz-series-item-{index}" : $"rz-series-item-{index} rz-series-item-{index % SankeyColorCount}";

        static string TooltipColorClass(this ColorScheme scheme, int index) =>
            $"rz-series-color-{scheme.ColorIndex(index)}-tooltip rz-series-shade-{scheme.Shade(index)}";
    }
}
