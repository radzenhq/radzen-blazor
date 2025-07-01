using Microsoft.AspNetCore.Components;
using System;
using Radzen;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPivotAggregate component. Must be placed inside a <see cref="RadzenPivotDataGrid{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotAggregate<TItem> : RadzenPivotField<TItem>, IDisposable
    {
        /// <summary>
        /// Gets or sets the aggregate function.
        /// </summary>
        [Parameter]
        public AggregateFunction Aggregate { get; set; } = AggregateFunction.Sum;

        /// <summary>
        /// Gets or sets the text align.
        /// </summary>
        [Parameter]
        public TextAlign TextAlign { get; set; } = TextAlign.Right;

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        [Parameter]
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets the IFormatProvider used for FormatString.
        /// </summary>
        [Parameter]
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// Gets or sets the column cell template rendered.
        /// </summary>
        [Parameter]
        public RenderFragment<object> Template { get; set; }

        /// <summary>
        /// Gets or sets the column total template rendered in the column footer.
        /// </summary>
        [Parameter]
        public RenderFragment<object> ColumnTotalTemplate { get; set; }

        /// <summary>
        /// Gets or sets the column total template rendered in the column footer.
        /// </summary>
        [Parameter]
        public RenderFragment<object> RowTotalTemplate { get; set; }

        /// <summary>
        /// Formats a value using the FormatString and FormatProvider.
        /// </summary>
        public string FormatValue(object value)
        {
            if (value == null)
                return "";

            if (!string.IsNullOrEmpty(FormatString))
            {
                return string.Format(FormatProvider ?? System.Globalization.CultureInfo.CurrentCulture, FormatString, value);
            }

            return value.ToString();
        }

        /// <summary>
        /// Called when the component is initialized. Registers this value with the parent pivot grid.
        /// </summary>
        protected override void OnInitialized()
        {
            if (PivotGrid != null)
            {
                PivotGrid.AddPivotAggregate(this);
            }
        }

        /// <summary>
        /// Disposes the component and removes it from the parent pivot grid.
        /// </summary>
        public void Dispose()
        {
            PivotGrid?.RemovePivotAggregate(this);
        }
    }
} 