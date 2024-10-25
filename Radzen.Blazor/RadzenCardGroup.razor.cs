using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCardGroup component.
    /// </summary>
    public partial class RadzenCardGroup : RadzenComponentWithChildren
    {
        /// <summary>
        /// Toggles the responsive mode of the component. If set to <c>true</c> (the default) the component will be 
        /// expanded on larger displays and collapsed on touch devices. Set to <c>false</c> if you want to disable this behavior.
        /// </summary>
        [Parameter]
        public bool Responsive { get; set; } = true;
        
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList = new List<string>();
            classList.Add("rz-card-group");

            if (Responsive)
            {
                classList.Add("rz-card-group-responsive");
            }

            return string.Join(" ", classList);
        }
    }
}