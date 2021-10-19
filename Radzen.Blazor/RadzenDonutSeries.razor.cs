using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenDonutSeries.
    /// Implements the <see cref="Radzen.Blazor.RadzenPieSeries{TItem}" />
    /// Implements the <see cref="Radzen.Blazor.IChartDonutSeries" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.Blazor.RadzenPieSeries{TItem}" />
    /// <seealso cref="Radzen.Blazor.IChartDonutSeries" />
    public partial class RadzenDonutSeries<TItem> : Radzen.Blazor.RadzenPieSeries<TItem>, Radzen.Blazor.IChartDonutSeries
    {
        /// <summary>
        /// Gets or sets the inner radius.
        /// </summary>
        /// <value>The inner radius.</value>
        [Parameter]
        public double? InnerRadius { get; set; }

        /// <summary>
        /// Gets or sets the title template.
        /// </summary>
        /// <value>The title template.</value>
        [Parameter]
        public RenderFragment TitleTemplate { get; set; }
    }
}