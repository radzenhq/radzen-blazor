using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A breadcrumb navigation component that displays the current page's location within the application hierarchy.
    /// RadzenBreadCrumb shows a trail of links representing the path from the root to the current page, helping users understand their location and navigate back.
    /// Provides secondary navigation with items separated by a visual divider (typically ">"), with each item linking to its respective page.
    /// Common uses include multi-level navigation indicating current location, e-commerce category navigation (Home > Electronics > Laptops), documentation section paths, and file system or folder navigation.
    /// Items are defined using RadzenBreadCrumbItem components as child content.
    /// The last item typically represents the current page and is often not clickable.
    /// </summary>
    /// <example>
    /// Basic breadcrumb:
    /// <code>
    /// &lt;RadzenBreadCrumb&gt;
    ///     &lt;RadzenBreadCrumbItem Text="Home" Path="/" /&gt;
    ///     &lt;RadzenBreadCrumbItem Text="Products" Path="/products" /&gt;
    ///     &lt;RadzenBreadCrumbItem Text="Laptops" /&gt;
    /// &lt;/RadzenBreadCrumb&gt;
    /// </code>
    /// Breadcrumb with icons and custom template:
    /// <code>
    /// &lt;RadzenBreadCrumb&gt;
    ///     &lt;RadzenBreadCrumbItem Text="Home" Path="/" Icon="home" /&gt;
    ///     &lt;RadzenBreadCrumbItem Text="Settings" Path="/settings" Icon="settings" /&gt;
    ///     &lt;RadzenBreadCrumbItem Text="Profile" Icon="person" /&gt;
    /// &lt;/RadzenBreadCrumb&gt;
    /// </code>
    /// </example>
    public partial class RadzenBreadCrumb : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets a custom template for rendering breadcrumb items.
        /// When set, this template is used instead of the default rendering for each item, allowing complete control over item appearance.
        /// The template receives a RadzenBreadCrumbItem as context.
        /// </summary>
        /// <value>The custom item template render fragment.</value>
        [Parameter]
        public RenderFragment<RadzenBreadCrumbItem>? Template { get; set; }

        /// <summary>
        /// Gets or sets the accessible label for the breadcrumb navigation landmark.
        /// Rendered as the <c>aria-label</c> attribute on the root <c>&lt;nav&gt;</c> element so assistive technology can distinguish this landmark from other navigation regions.
        /// </summary>
        /// <value>The navigation landmark label. Defaults to <c>"breadcrumb"</c>.</value>
        [Parameter]
        public string AriaLabel { get; set; } = "breadcrumb";

        /// <inheritdoc/>
        protected override string GetComponentCssClass()
        {
            return "rz-breadcrumb";
        }
    }

}
