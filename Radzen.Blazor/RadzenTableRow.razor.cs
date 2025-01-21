using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTableRow component.
    /// </summary>
    public partial class RadzenTableRow : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-data-row";
        }

        RadzenTable _table;

        /// <summary>
        /// Gets or sets the table.
        /// </summary>
        /// <value>The table.</value>
        [CascadingParameter]
        public RadzenTable Table
        {
            get
            {
                return _table;
            }
            set
            {
                if (_table != value)
                {
                    _table = value;
                    _table.AddRow(this);
                }
            }
        }

        List<RadzenTableCell> cells = new List<RadzenTableCell>();

        /// <summary>
        /// Adds the cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        public void AddCell(RadzenTableCell cell)
        {
            if (cells.IndexOf(cell) == -1)
            {
                cells.Add(cell);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        public void RemoveCell(RadzenTableCell cell)
        {
            if (cells.IndexOf(cell) != -1)
            {
                cells.Remove(cell);
                StateHasChanged();
            }
        }
    }
}
