using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A fieldset container component that groups related form fields with a legend/header and optional collapse functionality.
    /// RadzenFieldset provides semantic form grouping with visual borders, useful for organizing complex forms into logical sections.
    /// Fieldsets are HTML form elements that semantically group related inputs, improving form structure and accessibility.
    /// Features visual and semantic grouping of related form fields, customizable header via Text or HeaderTemplate, optional expand/collapse to hide/show grouped fields,
    /// optional icon in the legend, optional summary content shown when collapsed, and screen reader announcement of fieldset legends for grouped fields.
    /// Use to organize forms into sections like "Personal Information", "Address", "Payment Details". When AllowCollapse is enabled, users can collapse sections they don't need to see.
    /// </summary>
    /// <example>
    /// Basic fieldset grouping form fields:
    /// <code>
    /// &lt;RadzenFieldset Text="Personal Information"&gt;
    ///     &lt;RadzenStack Gap="1rem"&gt;
    ///         &lt;RadzenFormField Text="First Name"&gt;
    ///             &lt;RadzenTextBox @bind-Value=@model.FirstName /&gt;
    ///         &lt;/RadzenFormField&gt;
    ///         &lt;RadzenFormField Text="Last Name"&gt;
    ///             &lt;RadzenTextBox @bind-Value=@model.LastName /&gt;
    ///         &lt;/RadzenFormField&gt;
    ///     &lt;/RadzenStack&gt;
    /// &lt;/RadzenFieldset&gt;
    /// </code>
    /// Collapsible fieldset:
    /// <code>
    /// &lt;RadzenFieldset Text="Advanced Options" Icon="settings" AllowCollapse="true"&gt;
    ///     Advanced configuration fields...
    /// &lt;/RadzenFieldset&gt;
    /// </code>
    /// </example>
    public partial class RadzenFieldset : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => 
            ClassList.Create("rz-fieldset")
                     .Add("rz-fieldset-toggleable", AllowCollapse)
                     .ToString();

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

        async Task Toggle(EventArgs args)
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

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
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
