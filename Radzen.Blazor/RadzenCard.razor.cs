using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenCard : RadzenComponentWithChildren
    {
        protected override string GetComponentCssClass()
        {
            return "rz-card card";
        }
    }
}
