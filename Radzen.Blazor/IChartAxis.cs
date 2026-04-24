namespace Radzen.Blazor
{
    /// <summary>
    /// Common axis API of <see cref="RadzenChart" />
    /// </summary>
    public interface IChartAxis
    {
        /// <summary>
        /// Gets or sets the grid lines configuration of this axis.
        /// </summary>
        RadzenGridLines GridLines { get; set; }

        /// <summary>
        /// Gets or sets the crosshair configuration of this axis.
        /// </summary>
        RadzenAxisCrosshair Crosshair { get; set; }
    }
}