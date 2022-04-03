using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Bread Crumb Item Component
    /// </summary>
    public partial class RadzenBreadCrumbItem : RadzenComponent
    {
        /// <summary>
        /// Cascaded TEmplate Parameter from <see cref="RadzenBreadCrumb"/> Component
        /// </summary>
        [CascadingParameter]
        public RenderFragment<RadzenBreadCrumbItem> Template { get; set; }

        /// <summary>
        /// The Displayed Text
        /// </summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// An optional Link to be rendendered
        /// </summary>
        [Parameter]
        public string Path { get; set; }

        /// <summary>
        /// An optional Icon to be rendered
        /// </summary>
        [Parameter]
        public string Icon { get; set; }

        /// <inheritdoc/>
        protected override string GetComponentCssClass()
        {
            return "rz-breadcrumb-item";
        }
    }
}
