using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Non-generic contract for <see cref="RadzenSpiderChart"/> used by configuration components
    /// like <see cref="RadzenSpiderLegend"/> without relying on reflection (important for trimming/AOT).
    /// </summary>
    public interface IRadzenSpiderChart
    {
        /// <summary>
        /// Gets or sets the legend configuration for the chart.
        /// </summary>
        RadzenSpiderLegend Legend { get; set; }

        /// <summary>
        /// Requests the chart to refresh its rendering.
        /// </summary>
        Task Refresh();
    }
}

