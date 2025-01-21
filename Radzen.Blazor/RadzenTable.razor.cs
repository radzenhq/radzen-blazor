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
        /// <summary>
        /// Gets or sets the grid lines.
        /// </summary>
        /// <value>The grid lines.</value>
        [Parameter]
        public DataGridGridLines GridLines { get; set; } = DataGridGridLines.Default;

        /// <summary>
        /// Gets or sets a value indicating whether RadzenTable should use alternating row styles.
        /// </summary>
        /// <value><c>true</c> if RadzenTable is using alternating row styles; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowAlternatingRows { get; set; } = true;

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
            var styles = new List<string>(new string[] { "rz-grid-table" });

            if (AllowAlternatingRows)
            {
                styles.Add("rz-grid-table-striped");
            }

            if (GridLines != DataGridGridLines.Default)
            { 
                styles.Add($"rz-grid-gridlines-{Enum.GetName(typeof(DataGridGridLines), GridLines).ToLower()}");
            }

            return string.Join(" ", styles);
        }
    }
}
