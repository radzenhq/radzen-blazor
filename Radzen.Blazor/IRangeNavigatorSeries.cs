using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

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

        /// <summary>
        /// Gets the series data for the navigator tooltip: each point's X is its category in the input units
        /// of <paramref name="categoryScale" /> and its Y is its value. A series that returns no points shows no tooltip.
        /// </summary>
        IEnumerable<Point> GetDataPoints(ScaleBase categoryScale) => Enumerable.Empty<Point>();

        /// <summary>
        /// Gets the color of the tooltip's marker and border for this series' points.
        /// When <c>null</c>, the navigator's text color is used.
        /// </summary>
        string? Color => null;
    }
}
