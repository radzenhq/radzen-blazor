using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTableRow component.
    /// </summary>
    public partial class RadzenTableHeaderRow : RadzenComponentWithChildren
    {
        RadzenTableHeader _header;

        /// <summary>
        /// Gets or sets the table body.
        /// </summary>
        /// <value>The table body.</value>
        [CascadingParameter]
        public RadzenTableHeader Header
        {
            get
            {
                return _header;
            }
            set
            {
                if (_header != value)
                {
                    _header = value;
                    _header.AddRow(this);
                }
            }
        }

        List<RadzenTableHeaderCell> cells = new List<RadzenTableHeaderCell>();

        /// <summary>
        /// Adds the cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        public void AddCell(RadzenTableHeaderCell cell)
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
        public void RemoveCell(RadzenTableHeaderCell cell)
        {
            if (cells.IndexOf(cell) != -1)
            {
                cells.Remove(cell);
                StateHasChanged();
            }
        }
    }
}
