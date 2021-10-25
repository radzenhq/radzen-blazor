using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCard component.
    /// </summary>
    public partial class RadzenCard : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-card card";
        }
    }
}
