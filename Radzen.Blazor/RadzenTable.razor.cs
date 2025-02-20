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

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-data-grid rz-datatable rz-datatable-scrollable {(CurrentStyle.ContainsKey("height") ? "rz-has-height" : "")}".Trim();
        }

        /// <summary>
        /// Gets the table CSS classes.
        /// </summary>
        protected virtual string GetTableCssClass()
        {
            var styles = new List<string>(new string[] { "rz-grid-table", "rz-grid-table-fixed" });

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
