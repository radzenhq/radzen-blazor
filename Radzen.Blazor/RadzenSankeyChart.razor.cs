using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSankeyChart component.
    /// </summary>
    public partial class RadzenSankeyChart : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the color scheme of the chart.
        /// </summary>
        [Parameter]
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Pastel;

        /// <summary>
        /// Gets the actual width of the chart.
        /// </summary>
        protected double ActualWidth => Width;

        /// <summary>
        /// Gets the actual height of the chart.
        /// </summary>
        protected double ActualHeight => Height;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var colorScheme = ColorScheme.ToString().ToLower();
            return $"rz-sankey-chart rz-scheme-{colorScheme}";
        }
    }
}