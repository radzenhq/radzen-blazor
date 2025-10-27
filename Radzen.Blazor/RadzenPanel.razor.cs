using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A collapsible panel component with customizable header, content, summary, and footer sections.
    /// RadzenPanel provides an expandable/collapsible container for organizing and hiding content, ideal for settings panels, detail sections, or grouped information.
    /// Displays content in a structured container with optional collapsing functionality. When AllowCollapse is enabled, users can click the header to toggle the panel's expanded/collapsed state.
    /// Supports customizable header via HeaderTemplate/Text/Icon properties, main panel body via ChildContent, optional summary content shown when collapsed (SummaryTemplate),
    /// optional footer section (FooterTemplate), Collapsed property with two-way binding for programmatic control, and Expand/Collapse event callbacks.
    /// The header displays a collapse/expand icon when AllowCollapse is true, and users can click anywhere on the header to toggle.
    /// </summary>
    /// <example>
    /// Basic collapsible panel:
    /// <code>
    /// &lt;RadzenPanel Text="Advanced Options" Icon="settings" AllowCollapse="true"&gt;
    ///     &lt;RadzenStack Gap="1rem"&gt;
    ///         &lt;RadzenCheckBox @bind-Value=@option1 Text="Option 1" /&gt;
    ///         &lt;RadzenCheckBox @bind-Value=@option2 Text="Option 2" /&gt;
    ///     &lt;/RadzenStack&gt;
    /// &lt;/RadzenPanel&gt;
    /// </code>
    /// Panel with custom templates and events:
    /// <code>
    /// &lt;RadzenPanel AllowCollapse="true" @bind-Collapsed=@isCollapsed Expand=@OnExpand Collapse=@OnCollapse&gt;
    ///     &lt;HeaderTemplate&gt;
    ///         &lt;RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center"&gt;
    ///             &lt;RadzenIcon Icon="info" /&gt;
    ///             &lt;RadzenText TextStyle="TextStyle.H6"&gt;Details&lt;/RadzenText&gt;
    ///         &lt;/RadzenStack&gt;
    ///     &lt;/HeaderTemplate&gt;
    ///     &lt;ChildContent&gt;
    ///         Detailed content here...
    ///     &lt;/ChildContent&gt;
    ///     &lt;SummaryTemplate&gt;
    ///         &lt;RadzenText TextStyle="TextStyle.Caption"&gt;Click to expand details&lt;/RadzenText&gt;
    ///     &lt;/SummaryTemplate&gt;
    /// &lt;/RadzenPanel&gt;
    /// @code {
    ///     bool isCollapsed = false;
    ///     void OnExpand() => Console.WriteLine("Panel expanded");
    ///     void OnCollapse() => Console.WriteLine("Panel collapsed");
    /// }
    /// </code>
    /// </example>
    public partial class RadzenPanel : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-panel";
        }

        /// <summary>
        /// Gets or sets whether the panel can be collapsed by clicking its header.
        /// When enabled, a collapse/expand icon appears in the header, and clicking anywhere on the header toggles the panel state.
        /// </summary>
        /// <value><c>true</c> to allow collapsing; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool AllowCollapse { get; set; }

        private bool collapsed;

        /// <summary>
        /// Gets or sets whether the panel is currently in a collapsed state.
        /// When collapsed, the main content is hidden and only the header (and optional SummaryTemplate) are visible.
        /// Use with @bind-Collapsed for two-way binding to programmatically control the panel state.
        /// </summary>
        /// <value><c>true</c> if the panel is collapsed; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool Collapsed { get; set; }

        /// <summary>
        /// Gets or sets the Material icon name displayed in the panel header before the text.
        /// Use Material Symbols icon names (e.g., "settings", "info", "warning").
        /// </summary>
        /// <value>The Material icon name.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets a custom color for the header icon.
        /// Supports any valid CSS color value. If not set, uses the theme's default icon color.
        /// </summary>
        /// <value>The icon color as a CSS color value.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the text displayed in the panel header.
        /// This appears as the panel title. For more complex headers, use <see cref="HeaderTemplate"/> instead.
        /// </summary>
        /// <value>The header text. Default is empty string.</value>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the custom content for the panel header.
        /// When set, overrides the default header rendering (Text and Icon properties are ignored).
        /// Use this for complex headers with custom layouts, buttons, or other components.
        /// </summary>
        /// <value>The header template render fragment.</value>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the summary content displayed when the panel is collapsed.
        /// This optional content appears below the header in collapsed state, providing a preview or summary of the hidden content.
        /// When the panel is expanded, this content is not displayed.
        /// </summary>
        /// <value>The summary template render fragment.</value>
        [Parameter]
        public RenderFragment SummaryTemplate { get; set; } = null;

        /// <summary>
        /// Gets or sets the footer content displayed at the bottom of the panel.
        /// This section appears below the main content and remains visible regardless of collapse state.
        /// </summary>
        /// <value>The footer template render fragment.</value>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the panel is expanded from a collapsed state.
        /// Useful for loading data on-demand or triggering animations when the panel opens.
        /// </summary>
        /// <value>The expand event callback.</value>
        [Parameter]
        public EventCallback Expand { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the panel is collapsed from an expanded state.
        /// Useful for cleanup operations or tracking panel state changes.
        /// </summary>
        /// <value>The collapse event callback.</value>
        [Parameter]
        public EventCallback Collapse { get; set; }

        /// <summary>
        /// Gets or sets the title attribute of the expand button.
        /// </summary>
        /// <value>The title attribute value of the expand button.</value>
        [Parameter]
        public string ExpandTitle { get; set; } = "Expand";
        
        /// <summary>
        /// Gets or sets the title attribute of the collapse button.
        /// </summary>
        /// <value>The title attribute value of the collapse button.</value>
        [Parameter]
        public string CollapseTitle { get; set; } = "Collapse";

        /// <summary>
        /// Gets or sets the aria-label attribute of the expand button.
        /// </summary>
        /// <value>The aria-label attribute value of the expand button.</value>
        [Parameter]
        public string ExpandAriaLabel { get; set; } = null;
        
        /// <summary>
        /// Gets or sets the aria-label attribute of the collapse button.
        /// </summary>
        /// <value>The aria-label attribute value of the collapse button.</value>
        [Parameter]
        public string CollapseAriaLabel { get; set; } = null;
        
        async Task Toggle(MouseEventArgs args)
        {
            collapsed = !collapsed;

            if (collapsed)
            {
                await Collapse.InvokeAsync(args);
            }
            else
            {
                await Expand.InvokeAsync(args);
            }

            StateHasChanged();
        }
        string ContentClass => ClassList.Create("rz-panel-content-wrapper")
                                        .Add("rz-open", !collapsed)
                                        .Add("rz-close", collapsed)
                                        .ToString();

        string SummaryClass => ClassList.Create("rz-panel-content-wrapper")
                                        .Add("rz-open", collapsed)
                                        .Add("rz-close", !collapsed)
                                        .ToString();

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            collapsed = Collapsed;
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Collapsed), Collapsed))
            {
                collapsed = parameters.GetValueOrDefault<bool>(nameof(Collapsed));
            }

            await base.SetParametersAsync(parameters);
        }

        bool preventKeyPress = false;
        async Task OnKeyPress(KeyboardEventArgs args, Task task)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                await task;
            }
            else
            {
                preventKeyPress = false;
            }
        }
    }
}
