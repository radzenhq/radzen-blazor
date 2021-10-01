using System.Threading.Tasks;
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
        bool Visible { get; }
        bool ShowInLegend { get; }
        int RenderingOrder { get; set; }
        bool Contains(double x, double y, double tolerance);
        object DataAt(double x, double y);
        string Title { get; set; }
        string GetTitle();
        double MeasureLegend();
        Task InvokeClick(EventCallback<SeriesClickEventArgs> handler, object data);
    }
}
