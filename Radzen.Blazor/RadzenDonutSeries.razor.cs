using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders donut series in <see cref="RadzenChart" />.
    /// </summary>
    /// <typeparam name="TItem">The type of the series data item.</typeparam>
    [RequiresUnreferencedCode("The method references the various methods of the Expression class which are subject to trimming.")]
    public partial class RadzenDonutSeries<TItem> : RadzenPieSeries<TItem>, IChartDonutSeries
    {
        /// <summary>
        /// Gets or sets the inner radius of the donut.
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
