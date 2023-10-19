using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenStepsItem component.
    /// </summary>
    public class RadzenStepsItem : RadzenComponent
    {
        private string _text;
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    Steps?.Refresh();
                }
            }
        }

        /// <summary>
        /// Gets or sets the title attribute.
        /// </summary>
        public string Title { get; set; } = null;
        
        /// <summary>
        /// Gets or sets the aria-label attribute.
        /// </summary>
        public string AriaLabel { get; set; } = null;
        
        /// <summary>
        /// Gets or sets text of the next button.
        /// </summary>
        [Parameter]
        public string NextText { get; set; } = null;

        /// <summary>
        /// Gets or sets the title attribute of the next button.
        /// </summary>
        public string NextTitle { get; set; } = null;
        
        /// <summary>
        /// Gets or sets the aria-label attribute of the next button.
        /// </summary>
        public string NextAriaLabel { get; set; } = null;
        
        /// <summary>
        /// Gets or sets text of the previous button.
        /// </summary>
        [Parameter]
        public string PreviousText { get; set; } = null;

        /// <summary>
        /// Gets or sets the title attribute of the previous button.
        /// </summary>
        public string PreviousTitle { get; set; } = null;
        
        /// <summary>
        /// Gets or sets the aria-label attribute of the previous button.
        /// </summary>
        public string PreviousAriaLabel { get; set; } = null;
        
        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<RadzenStepsItem> Template { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenStepsItem"/> is selected.
        /// </summary>
        /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Selected { get; set; }

        bool _visible = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenComponent" /> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public override bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    Steps?.Refresh();
                }
            }
        }

        bool _disabled;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenStepsItem"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled
        {
            get
            {
                return _disabled;
            }
            set
            {
                if (_disabled != value)
                {
                    _disabled = value;
                    Steps?.Refresh();
                }
            }
        }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        RadzenSteps _steps;

        /// <summary>
        /// Gets or sets the steps.
        /// </summary>
        /// <value>The steps.</value>
        [CascadingParameter]
        public RadzenSteps Steps
        {
            get
            {
                return _steps;
            }
            set
            {
                if (_steps != value)
                {
                    _steps = value;
                    _steps.AddStep(this);
                }
            }
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Selected), Selected))
            {
                var selected = parameters.GetValueOrDefault<bool>(nameof(Selected));
                if (!selected)
                {
                    Steps?.SelectFirst();
                }
                else
                {
                    Steps?.SelectStep(this);
                }
            }

            await base.SetParametersAsync(parameters);
        }

        internal string GetItemCssClass()
        {
            return GetCssClass();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-steps-item {(Steps.StepsCollection.IndexOf(this) == Steps.SelectedIndex ? "rz-state-highlight rz-steps-current" : string.Empty)} {(Disabled ? "rz-state-disabled" : string.Empty)}";
        }
    }
}