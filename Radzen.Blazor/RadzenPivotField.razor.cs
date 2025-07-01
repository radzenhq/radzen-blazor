using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base component for RadzenPivotDataGrid Rows, Columns and Aggregates.
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotField<TItem> : ComponentBase
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
        /// Gets or sets a value indicating whether this column is visible.
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        [Parameter]
        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this column is sortable.
        /// </summary>
        [Parameter]
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// Gets the column title.
        /// </summary>
        public string GetTitle()
        {
            return !string.IsNullOrEmpty(Title) ? Title : Property;
        }

        /// <summary>
        /// Gets or sets the parent pivot data grid component.
        /// </summary>
        [CascadingParameter]
        public RadzenPivotDataGrid<TItem> PivotGrid { get; set; }
    }
} 