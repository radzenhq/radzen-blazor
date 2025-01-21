using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTableRow component.
    /// </summary>
    public partial class RadzenTableHeaderCell : RadzenComponentWithChildren
    {
        RadzenTableHeaderRow _row;

        /// <summary>
        /// Gets or sets the row.
        /// </summary>
        /// <value>The row.</value>
        [CascadingParameter]
        public RadzenTableHeaderRow Row
        {
            get
            {
                return _row;
            }
            set
            {
                if (_row != value)
                {
                    _row = value;
                    _row.AddCell(this);
                }
            }
        }
    }
}
