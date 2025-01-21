using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTableHeader component.
    /// </summary>
    public partial class RadzenTableHeader : RadzenComponentWithChildren
    {
        List<RadzenTableHeaderRow> rows = new List<RadzenTableHeaderRow>();

        /// <summary>
        /// Adds the row.
        /// </summary>
        /// <param name="row">The row.</param>
        public void AddRow(RadzenTableHeaderRow row)
        {
            if (rows.IndexOf(row) == -1)
            {
                rows.Add(row);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the row.
        /// </summary>
        /// <param name="row">The row.</param>
        public void RemoveRow(RadzenTableHeaderRow row)
        {
            if (rows.IndexOf(row) != -1)
            {
                rows.Remove(row);
                StateHasChanged();
            }
        }
    }
}
