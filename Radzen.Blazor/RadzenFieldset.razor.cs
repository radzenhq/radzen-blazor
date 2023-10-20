using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenFieldset component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenFieldset AllowCollapse="true""&gt;
    ///     &lt;HeaderTemplate&gt;
    ///         Header
    ///     &lt;/HeaderTemplate&gt;
    ///     &lt;ChildContent&gt;
    ///         Content
    ///     &lt;/ChildContent&gt;
    ///     &lt;SummaryTemplate&gt;
    ///         Summary
    ///     &lt;/SummaryTemplate&gt;
    /// &lt;/RadzenFieldset&gt;
    /// </code>
    /// </example>
    public partial class RadzenFieldset : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return AllowCollapse ? "rz-fieldset rz-fieldset-toggleable" : "rz-fieldset";
        }

        /// <summary>
        /// Gets or sets a value indicating whether collapsing is allowed. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if collapsing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowCollapse { get; set; }

        private bool collapsed;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenFieldset"/> is collapsed.
        /// </summary>
        /// <value><c>true</c> if collapsed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Collapsed { get; set; }

        /// <summary>
        /// Gets or sets the title attribute of the expand button.
        /// </summary>
        /// <value>The title attribute value of the expand button.</value>
        [Parameter]
        public string ExpandTitle { get; set; }
        
        /// <summary>
        /// Gets or sets the title attribute of the collapse button.
        /// </summary>
        /// <value>The title attribute value of the collapse button.</value>
        [Parameter]
        public string CollapseTitle { get; set; }
        
        /// <summary>
        /// Gets or sets the aria-label attribute of the expand button.
        /// </summary>
        /// <value>The aria-label attribute value of the expand button.</value>
        [Parameter]
        public string ExpandAriaLabel { get; set; }
        
        /// <summary>
        /// Gets or sets the aria-label attribute of the collapse button.
        /// </summary>
        /// <value>The aria-label attribute value of the collapse button.</value>
        [Parameter]
        public string CollapseAriaLabel { get; set; }
        
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
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the summary template.
        /// </summary>
        /// <value>The summary template.</value>
        [Parameter]
        public RenderFragment SummaryTemplate { get; set; } = null;

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

        string contentStyle = "";
        string summaryContentStyle = "display: none";

        async System.Threading.Tasks.Task Toggle(EventArgs args)
        {
            collapsed = !collapsed;
            contentStyle = collapsed ? "display: none;" : "";
            summaryContentStyle = !collapsed ? "display: none" : "";

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

        private string TitleAttribute()
        {
            if (collapsed)
            {
                return string.IsNullOrWhiteSpace(ExpandTitle) ? "Expand" : ExpandTitle;
            }
            return string.IsNullOrWhiteSpace(CollapseTitle) ? "Collapse" : CollapseTitle;
        }
        
        private string AriaLabelAttribute()
        {
            if (collapsed)
            {
                return string.IsNullOrWhiteSpace(ExpandAriaLabel) ? "Expand" : ExpandAriaLabel;  
            }
            return string.IsNullOrWhiteSpace(CollapseAriaLabel) ? "Collapse" : CollapseAriaLabel;
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

        /// <inheritdoc />
        protected override Task OnParametersSetAsync()
        {
            contentStyle = collapsed ? "display: none;" : "";
            summaryContentStyle = !collapsed ? "display: none" : "";

            return base.OnParametersSetAsync();
        }
    }
}
