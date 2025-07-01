using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPivotRow component. Must be placed inside a <see cref="RadzenPivotDataGrid{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotRow<TItem> : RadzenPivotField<TItem>, IDisposable
    {
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