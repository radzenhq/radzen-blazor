using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class of Radzen progress components.
    /// </summary>
    public abstract class ProgressBase : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The  template.</value>
        [Parameter]
        public RenderFragment Template { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        /// <value>The unit.</value>
        [Parameter]
        public string Unit { get; set; } = "%";

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public double Value { get; set; }

        /// <summary>
        /// Determines the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        [Parameter]
        public double Min { get; set; } = 0;

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        [Parameter]
        public double Max { get; set; } = 100;

        /// <summary>
        /// Gets or sets a value indicating whether value is shown.
        /// </summary>
        /// <value><c>true</c> if value is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the value changed callback.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public Action<double> ValueChanged { get; set; }

        /// <summary>
        /// Progress in range from 0 to 1.
        /// </summary>
        protected double NormalizedValue => Math.Min(Math.Max((Value - Min) / (Max - Min), 0), 1);

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public void Refresh()
            => StateHasChanged();
    }
}
