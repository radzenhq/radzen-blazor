using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPivotAggregate component. Must be placed inside a <see cref="RadzenPivotDataGrid{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotAggregate<TItem> : ComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        [Parameter]
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the aggregate function.
        /// </summary>
        [Parameter]
        public AggregateFunction Aggregate { get; set; } = AggregateFunction.Sum;

        /// <summary>
        /// Gets or sets a value indicating whether this value is visible.
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = true;

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
        /// Gets or sets the template.
        /// </summary>
        [Parameter]
        public RenderFragment<object> Template { get; set; }

        /// <summary>
        /// Gets or sets the header template.
        /// </summary>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// Gets the value title.
        /// </summary>
        public string GetTitle()
        {
            return !string.IsNullOrEmpty(Title) ? Title : Property;
        }

        /// <summary>
        /// Gets the value for specified item.
        /// </summary>
        public virtual object GetValue(TItem item)
        {
            if (string.IsNullOrEmpty(Property))
                return "";

            return PropertyAccess.GetValue(item, Property);
        }

        /// <summary>
        /// Gets the header content.
        /// </summary>
        public object GetHeader()
        {
            if (HeaderTemplate != null)
            {
                return HeaderTemplate;
            }
            else
            {
                return GetTitle();
            }
        }

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
        /// Gets or sets the parent pivot data grid component.
        /// </summary>
        [CascadingParameter]
        public RadzenPivotDataGrid<TItem> PivotGrid { get; set; }

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

    /// <summary>
    /// Specifies the aggregate function for pivot values.
    /// </summary>
    public enum AggregateFunction
    {
        /// <summary>
        /// Sum of values.
        /// </summary>
        Sum,
        /// <summary>
        /// Count of items.
        /// </summary>
        Count,
        /// <summary>
        /// Average of values.
        /// </summary>
        Average,
        /// <summary>
        /// Minimum value.
        /// </summary>
        Min,
        /// <summary>
        /// Maximum value.
        /// </summary>
        Max,
        /// <summary>
        /// First value.
        /// </summary>
        First,
        /// <summary>
        /// Last value.
        /// </summary>
        Last
    }
} 