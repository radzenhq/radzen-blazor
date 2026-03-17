using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Represents a series that can be rendered inside a <see cref="RadzenRangeNavigator" />.
    /// </summary>
    public interface IRangeNavigatorSeries
    {
        /// <summary>
        /// Transforms the category scale based on the series data.
        /// </summary>
        ScaleBase TransformCategoryScale(ScaleBase scale);

        /// <summary>
        /// Transforms the value scale based on the series data.
        /// </summary>
        ScaleBase TransformValueScale(ScaleBase scale);

        /// <summary>
        /// Renders the series using the specified scales.
        /// </summary>
        RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);
    }
}
