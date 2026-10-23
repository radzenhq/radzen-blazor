using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Marker interface for <see cref="RadzenStackedBarSeries{TItem}" />.
    /// </summary>
    public interface IChartStackedBarSeries : IChartBarSeries
    {
        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        double ValueAt(int index);

        /// <summary>
        /// Gets the values for category.
        /// </summary>
        IEnumerable<double> ValuesForCategory(double category);
        /// <summary>
        /// Gets the items for category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        IEnumerable<object> ItemsForCategory(double category);

        /// <summary>
        /// Gets the stack group of the series. Series with the same group stack on top of each other; every distinct group renders as its own column or bar.
        /// </summary>
        string? StackGroup => null;
    }
}