using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A component to display a Bread Crumb style menu
    /// </summary>
    public partial class RadzenBreadCrumb : RadzenComponentWithChildren
    {
        /// <summary>
        /// An optional RenderFragment that is rendered per Item
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenBreadCrumbItem> Template { get; set; }

        /// <inheritdoc/>
        protected override string GetComponentCssClass()
        {
            return "rz-breadcrumb";
        }
    }

}
