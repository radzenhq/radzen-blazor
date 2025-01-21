using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTableBody component.
    /// </summary>
    public partial class RadzenTableBody : RadzenComponentWithChildren
    {
        List<RadzenTableRow> rows = new List<RadzenTableRow>();

        /// <summary>
        /// Adds the row.
        /// </summary>
        /// <param name="row">The row.</param>
        public void AddRow(RadzenTableRow row)
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
        public void RemoveRow(RadzenTableRow row)
        {
            if (rows.IndexOf(row) != -1)
            {
                rows.Remove(row);
                StateHasChanged();
            }
        }
    }
}
