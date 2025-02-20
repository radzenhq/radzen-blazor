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

        RadzenTableBody _body;

        /// <summary>
        /// Gets or sets the table body.
        /// </summary>
        /// <value>The table body.</value>
        [CascadingParameter]
        public RadzenTableBody Body
        {
            get
            {
                return _body;
            }
            set
            {
                if (_body != value)
                {
                    _body = value;
                    _body.AddRow(this);
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
