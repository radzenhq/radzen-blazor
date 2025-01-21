using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTableRow component.
    /// </summary>
    public partial class RadzenTableCell : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-data-cell";
        }

        RadzenTableRow _row;

        /// <summary>
        /// Gets or sets the row.
        /// </summary>
        /// <value>The row.</value>
        [CascadingParameter]
        public RadzenTableRow Row
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
