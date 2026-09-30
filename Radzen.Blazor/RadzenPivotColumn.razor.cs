using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPivotColumn component. Must be placed inside a <see cref="RadzenPivotDataGrid{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotColumn<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] TItem> : RadzenPivotField<TItem>, IDisposable
    {
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [Parameter]
        public string? Width { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of column groups to display at this level. When the number of distinct values exceeds this limit
        /// the most significant groups (ranked by the sorted aggregate or the first aggregate) are kept and the remaining items
        /// are combined into a single group labeled with <see cref="OthersLabel"/>. Set to <c>null</c> (the default) to display all groups.
        /// </summary>
        [Parameter]
        public int? MaxGroups { get; set; }

        /// <summary>
        /// Gets or sets the label of the group that combines the remaining items when <see cref="MaxGroups"/> is exceeded.
        /// Set to <c>null</c> by default - the localized <see cref="RadzenPivotDataGrid{TItem}.OthersText"/> of the parent pivot grid is used.
        /// </summary>
        [Parameter]
        public string? OthersLabel { get; set; }

        /// <summary>
        /// Called when the component is initialized. Registers this column with the parent pivot grid.
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (PivotGrid != null)
            {
                PivotGrid.AddPivotColumn(this);
            }
        }

        /// <summary>
        /// Disposes the component and removes it from the parent pivot grid.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            PivotGrid?.RemovePivotColumn(this);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets the header template.
        /// </summary>
        /// <value>The header template.</value>
        [Parameter]
        public RenderFragment<GroupResult>? HeaderTemplate { get; set; }
    }
} 