using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenFieldset component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenFieldset : RadzenComponent
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return AllowCollapse ? "rz-fieldset rz-fieldset-toggleable" : "rz-fieldset";
        }

        /// <summary>
        /// Gets or sets a value indicating whether collapse is allowed.
        /// </summary>
        /// <value><c>true</c> if [allow collapse]; otherwise, <c>false</c>.</value>
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
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

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

        /// <summary>
        /// Called when initialized.
        /// </summary>
        protected override void OnInitialized()
        {
            collapsed = Collapsed;
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Collapsed), Collapsed))
            {
                collapsed = parameters.GetValueOrDefault<bool>(nameof(Collapsed));
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Called when parameters are set asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task OnParametersSetAsync()
        {
            contentStyle = collapsed ? "display: none;" : "";
            summaryContentStyle = !collapsed ? "display: none" : "";

            return base.OnParametersSetAsync();
        }
    }
}