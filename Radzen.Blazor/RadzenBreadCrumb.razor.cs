using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A component to display a Bread Crumb style menu
    /// </summary>
    public partial class RadzenBreadCrumb : RadzenComponent
    {

        /// <summary>
        /// An optional RenderFragment that is rendered per Item
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenBreadCrumbItem> Template { get; set; }

        /// <summary>
        /// Items to be Displayed
        /// </summary>
        [Parameter]
        public RenderFragment Items { get; set; }

    }

}
