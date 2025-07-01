using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPivotRow component. Must be placed inside a <see cref="RadzenPivotDataGrid{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotRow<TItem> : ComponentBase, IDisposable
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
        /// Gets or sets a value indicating whether this row is visible.
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        [Parameter]
        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this row is sortable.
        /// </summary>
        [Parameter]
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// Gets or sets the text align.
        /// </summary>
        [Parameter]
        public TextAlign TextAlign { get; set; } = TextAlign.Left;

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        [Parameter]
        public RenderFragment<TItem> Template { get; set; }

        /// <summary>
        /// Gets or sets the header template.
        /// </summary>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// Gets the row title.
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
        /// Gets or sets the parent pivot data grid component.
        /// </summary>
        [CascadingParameter]
        public RadzenPivotDataGrid<TItem> PivotGrid { get; set; }

        /// <summary>
        /// Called when the component is initialized. Registers this row with the parent pivot grid.
        /// </summary>
        protected override void OnInitialized()
        {
            if (PivotGrid != null)
            {
                PivotGrid.AddPivotRow(this);
            }
        }

        /// <summary>
        /// Disposes the component and removes it from the parent pivot grid.
        /// </summary>
        public void Dispose()
        {
            PivotGrid?.RemovePivotRow(this);
        }
    }
} 