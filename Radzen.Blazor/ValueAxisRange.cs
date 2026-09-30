namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies how a value axis of <see cref="RadzenChart" /> determines its range from the plotted series. Set via <see cref="RadzenValueAxis.Range" />.
    /// </summary>
    public enum ValueAxisRange
    {
        /// <summary>
        /// The axis includes zero when it displays a series filled from a baseline - column, bar, area, their stacked and full-stacked variants,
        /// and waterfall - and otherwise fits the plotted values. A <see cref="RadzenSparkline" /> fits the plotted values as with <see cref="Data" />.
        /// This is the default.
        /// </summary>
        Auto,
        /// <summary>
        /// The axis fits the plotted values, so column, bar and area series whose values do not reach zero are cut off; their stacked and
        /// full-stacked variants and waterfall series still reach zero because their values are totals accumulated from zero.
        /// </summary>
        Data,
        /// <summary>
        /// The axis extends to include zero for every series, so all-positive values start at zero and all-negative values end at zero.
        /// </summary>
        IncludeZero
    }
}
