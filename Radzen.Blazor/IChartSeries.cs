using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public interface IChartSeries
    {
        ScaleBase TransformCategoryScale(ScaleBase scale);
        ScaleBase TransformValueScale(ScaleBase scale);
        RadzenMarkers Markers { get; set; }
        MarkerType MarkerType { get; }
        double MarkerSize { get; }
        RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);
        RenderFragment RenderTooltip(object data, double marginLeft, double marginTop);
        RenderFragment RenderLegendItem();
        string Color { get; }
        bool Visible { get; set; }
        int RenderingOrder { get; set; }
        bool Contains(double x, double y);
        object DataAt(double x, double y);
        string Title { get; set; }
        string GetTitle();
        double MeasureLegend();
    }
}
