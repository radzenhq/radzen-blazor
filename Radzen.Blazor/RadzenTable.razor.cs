using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTable component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTable&gt;
    ///     &lt;RadzenTableRow&gt;
    ///         &lt;RadzenTableCell&gt;
    ///         &lt;/RadzenTableCell&gt;
    ///     &lt;/RadzenTableRow&gt;
    /// &lt;/RadzenTable&gt;
    /// </code>
    /// </example>
    public partial class RadzenTable : RadzenComponentWithChildren
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

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-grid-table";
        }
    }
}
