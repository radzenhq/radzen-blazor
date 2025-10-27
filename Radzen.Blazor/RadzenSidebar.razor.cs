using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A collapsible sidebar component for application navigation, typically used within RadzenLayout.
    /// RadzenSidebar provides a navigation panel that can be toggled open/closed and responds to screen size changes.
    /// Commonly used in application layouts for primary navigation menus.
    /// Features responsive design (automatically collapses on mobile devices and expands on desktop, configurable), positioning on Left/Right/Start/End of the layout,
    /// full height option to span entire layout height or align with body section only, programmatic expand/collapse via Expanded property or Toggle() method,
    /// seamless integration with RadzenLayout/RadzenHeader/RadzenBody/RadzenFooter, and typically contains RadzenPanelMenu or RadzenMenu for navigation.
    /// Must be used inside RadzenLayout to enable responsive behavior and proper layout integration. Use @bind-Expanded for two-way binding to control sidebar state from code.
    /// </summary>
    /// <example>
    /// Basic sidebar with menu:
    /// <code>
    /// &lt;RadzenLayout&gt;
    ///     &lt;RadzenHeader&gt;...&lt;/RadzenHeader&gt;
    ///     &lt;RadzenSidebar @bind-Expanded=@sidebarExpanded&gt;
    ///         &lt;RadzenPanelMenu&gt;
    ///             &lt;RadzenPanelMenuItem Text="Home" Icon="home" Path="/" /&gt;
    ///             &lt;RadzenPanelMenuItem Text="Orders" Icon="shopping_cart" Path="/orders" /&gt;
    ///         &lt;/RadzenPanelMenu&gt;
    ///     &lt;/RadzenSidebar&gt;
    ///     &lt;RadzenBody&gt;...&lt;/RadzenBody&gt;
    /// &lt;/RadzenLayout&gt;
    /// </code>
    /// Right-positioned full-height sidebar:
    /// <code>
    /// &lt;RadzenSidebar Position="SidebarPosition.Right" FullHeight="true"&gt;
    ///     Sidebar content...
    /// &lt;/RadzenSidebar&gt;
    /// </code>
    /// </example>
    public partial class RadzenSidebar : RadzenComponentWithChildren
    {
        private const string DefaultStyle = "top:51px;bottom:57px;width:250px;";

        /// <summary>
        /// Gets or sets custom CSS styles for the sidebar.
        /// Default positioning and sizing can be overridden via this property.
        /// </summary>
        /// <value>The CSS style string. Default is "top:51px;bottom:57px;width:250px;".</value>
        [Parameter]
        public override string Style { get; set; } = DefaultStyle;

        /// <summary>
        /// Gets or sets whether the sidebar should automatically collapse on small screens (responsive mode).
        /// When true (default), the sidebar expands on desktop and collapses on mobile. When false, responsive behavior is disabled.
        /// Responsive mode only works when RadzenSidebar is inside <see cref="RadzenLayout"/>.
        /// </summary>
        /// <value><c>true</c> to enable responsive collapse/expand; <c>false</c> to disable. Default is <c>true</c>.</value>
        [Parameter]
        public bool Responsive { get; set; } = true;

        private bool IsResponsive => Responsive && Layout != null;

        /// <summary>
        /// Gets or sets whether the sidebar occupies the full height of the layout (from top to bottom).
        /// When false (default), sidebar appears between header and footer, aligned with body section.
        /// When true, sidebar stretches the entire layout height alongside all sections.
        /// </summary>
        /// <value><c>true</c> for full-height sidebar; <c>false</c> for body-aligned sidebar. Default is <c>false</c>.</value>
        [Parameter]
        public bool FullHeight { get; set; } = false;

        /// <summary>
        /// Gets or sets which side of the layout the sidebar appears on.
        /// Options include Left, Right, Start (left in LTR, right in RTL), End (right in LTR, left in RTL).
        /// </summary>
        /// <value>The sidebar position. Default is <see cref="SidebarPosition.Start"/>.</value>
        [Parameter]
        public SidebarPosition? Position { get; set; } = SidebarPosition.Start;

        /// <summary>
        /// The <see cref="RadzenLayout" /> this component is nested in.
        /// </summary>
        [CascadingParameter]
        public RadzenLayout Layout { get; set; }

        /// <summary>
        /// Gets or sets the maximum width, in pixels, at which the component switches to a responsive layout.
        /// </summary>
        [Parameter]
        public string ResponsiveMaxWidth { get; set; } = "768px";
        private string Query => $"(max-width: {ResponsiveMaxWidth})";

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-sidebar").Add("rz-sidebar-expanded", expanded == true)
                                                          .Add("rz-sidebar-collapsed", expanded == false)
                                                          .Add("rz-sidebar-responsive", IsResponsive)
                                                          .Add("rz-sidebar-fullheight", FullHeight)
                                                          .Add($"rz-sidebar-{Position.Value.ToString().ToLower()}")
                                                          .ToString();
        }

        /// <summary>
        /// Programmatically toggles the sidebar between expanded and collapsed states.
        /// Call this method to show/hide the sidebar from code, such as from a hamburger menu button click.
        /// </summary>
        public void Toggle()
        {
            expanded = Expanded = !Expanded;

            StateHasChanged();
        }

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string GetStyle()
        {
            var style = Style;

            if (Layout != null && !string.IsNullOrEmpty(style))
            {
                style = style.Replace(DefaultStyle, "");
            }

            if (Layout != null)
            {
                return style;
            }

            return $"{style}{(Expanded ? ";transform:translateX(0px);" : ";width:0px;transform:translateX(-100%);")}";
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSidebar"/> is expanded.
        /// </summary>
        /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Expanded { get; set; } = true;

        /// <summary>
        /// Gets or sets the expanded changed callback.
        /// </summary>
        /// <value>The expanded changed callback.</value>
        [Parameter]
        public EventCallback<bool> ExpandedChanged { get; set; }

        bool? expanded;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            if (!Responsive)
            {
                expanded = Expanded;
            }

            base.OnInitialized();
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Expanded), Expanded))
            {
                expanded = parameters.GetValueOrDefault<bool>(nameof(Expanded));
            }

            await base.SetParametersAsync(parameters);
        }

        async Task OnChange(bool matches)
        {
            expanded = !matches;
            await ExpandedChanged.InvokeAsync(!matches);
        }
    }
}
