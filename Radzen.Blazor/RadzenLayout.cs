using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// A layout container component that defines the overall structure of a Blazor application with header, sidebar, body, and footer sections.
    /// RadzenLayout is typically used in MainLayout.razor to create a consistent page structure with optional collapsible sidebar and theme integration.
    /// Works with companion components: RadzenHeader, RadzenSidebar, RadzenBody, and RadzenFooter. Automatically integrates with ThemeService to apply theme-specific CSS classes.
    /// All sections are optional and can be used in any combination to create the desired page structure. The sidebar can be configured as collapsible, and the layout adjusts automatically when the sidebar expands or collapses.
    /// </summary>
    /// <example>
    /// Basic layout with all sections:
    /// <code>
    /// &lt;RadzenLayout&gt;
    ///     &lt;RadzenHeader&gt;
    ///         &lt;h1&gt;My Application&lt;/h1&gt;
    ///     &lt;/RadzenHeader&gt;
    ///     &lt;RadzenSidebar&gt;
    ///         &lt;RadzenPanelMenu&gt;
    ///             @* Navigation menu items *@
    ///         &lt;/RadzenPanelMenu&gt;
    ///     &lt;/RadzenSidebar&gt;
    ///     &lt;RadzenBody&gt;
    ///         @Body
    ///     &lt;/RadzenBody&gt;
    ///     &lt;RadzenFooter&gt;
    ///         © 2025 My Company
    ///     &lt;/RadzenFooter&gt;
    /// &lt;/RadzenLayout&gt;
    /// </code>
    /// </example>
    public partial class RadzenLayout : RadzenComponentWithChildren
    {
        [Inject]
        private IServiceProvider ServiceProvider { get; set; }

        private ThemeService themeService;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            themeService = ServiceProvider.GetService<ThemeService>();

            if (themeService != null)
            {
                themeService.ThemeChanged += OnThemeChanged;
            }

            base.OnInitialized();
        }

        private void OnThemeChanged()
        {
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-layout")
            .Add($"rz-{themeService?.Theme}", themeService != null)
            .ToString();
    }
}