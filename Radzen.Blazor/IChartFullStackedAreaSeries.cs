using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Marker interface for <see cref="RadzenFullStackedAreaSeries{TItem}" />.
    /// </summary>
    public interface IChartFullStackedAreaSeries
    {
        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        int Count { get; }

        /// <summary>
        /// Gets the values for category.
        /// </summary>
        IEnumerable<double> ValuesForCategory(double category);

        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        double ValueAt(int index);
    }
}
