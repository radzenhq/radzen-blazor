using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    interface IChartCategorySeries
    {
        (object?, Point) DataAtCategory(double x);
    }
}
