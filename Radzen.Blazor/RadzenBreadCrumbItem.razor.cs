using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Bread Crumb Item Component
    /// </summary>
    public partial class RadzenBreadCrumbItem : RadzenComponent
    {
        /// <summary>
        /// Cascaded Template Parameter from <see cref="RadzenBreadCrumb"/> Component
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

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        /// <value>The icon color.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Template Parameter used only for this Item
        /// Note: this overrides the <see cref="Template"/> Cascading Parameter
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <inheritdoc/>
        protected override string GetComponentCssClass()
        {
            return "rz-breadcrumb-item";
        }
    }
}
