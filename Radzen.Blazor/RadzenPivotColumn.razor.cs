using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPivotColumn component. Must be placed inside a <see cref="RadzenPivotDataGrid{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotColumn<TItem> : RadzenPivotField<TItem>, IDisposable
    {
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [Parameter]
        public string Width { get; set; }

        /// <summary>
        /// Called when the component is initialized. Registers this column with the parent pivot grid.
        /// </summary>
        protected override void OnInitialized()
        {
            if (PivotGrid != null)
            {
                PivotGrid.AddPivotColumn(this);
            }
        }

        /// <summary>
        /// Disposes the component and removes it from the parent pivot grid.
        /// </summary>
        public void Dispose()
        {
            PivotGrid?.RemovePivotColumn(this);
        }
    }
} 