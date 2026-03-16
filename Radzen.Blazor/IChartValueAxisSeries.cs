namespace Radzen.Blazor
{
    /// <summary>
    /// Implemented by series that can be bound to a named value axis.
    /// </summary>
    public interface IChartValueAxisSeries
    {
        /// <summary>
        /// Gets or sets the name of the value axis this series is bound to.
        /// When null or empty, the series uses the primary (default) axis.
        /// </summary>
        string? ValueAxisName { get; set; }
    }
}
