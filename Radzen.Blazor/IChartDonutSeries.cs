using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public interface IChartDonutSeries
    {
      RenderFragment RenderTitle(double x, double y);
    }
}