using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPanel component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenPanel AllowCollapse="true""&gt;
    ///     &lt;HeaderTemplate&gt;
    ///         Header
    ///     &lt;/HeaderTemplate&gt;
    ///     &lt;ChildContent&gt;
    ///         Content
    ///     &lt;/ChildContent&gt;
    ///     &lt;SummaryTemplate&gt;
    ///         Summary
    ///     &lt;/SummaryTemplate&gt;
    /// &lt;/RadzenPanel&gt;
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
        /// Gets or sets a value indicating whether collapsing is allowed. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if collapsing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowCollapse { get; set; }

        private bool collapsed;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenPanel"/> is collapsed.
        /// </summary>
        /// <value><c>true</c> if collapsed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Collapsed { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        /// <value>The icon color.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the header template.
        /// </summary>
        /// <value>The header template.</value>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the summary template.
        /// </summary>
        /// <value>The summary template.</value>
        [Parameter]
        public RenderFragment SummaryTemplate { get; set; } = null;

        /// <summary>
        /// Gets or sets the footer template.
        /// </summary>
        /// <value>The footer template.</value>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the expand callback.
        /// </summary>
        /// <value>The expand callback.</value>
        [Parameter]
        public EventCallback Expand { get; set; }

        /// <summary>
        /// Gets or sets the collapse callback.
        /// </summary>
        /// <value>The collapse callback.</value>
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
        
        string contentStyle = "display: block;";
        string summaryContentStyle = "display: none";

        async System.Threading.Tasks.Task Toggle(MouseEventArgs args)
        {
            collapsed = !collapsed;
            contentStyle = collapsed ? "display: none;" : "display: block;";
            summaryContentStyle = !collapsed ? "display: none" : "display: block";

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

        /// <inheritdoc />
        protected override void OnInitialized()
        {
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

        /// <summary>
        /// Called when parameters set asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task OnParametersSetAsync()
        {
            contentStyle = collapsed ? "display: none;" : "display: block;";
            summaryContentStyle = !collapsed ? "display: none" : "display: block";

            return base.OnParametersSetAsync();
        }
    }
}
