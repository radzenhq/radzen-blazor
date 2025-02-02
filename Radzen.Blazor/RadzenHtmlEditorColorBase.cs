using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class that RadzenHtmlEditor color picker tools inherit from.
    /// </summary>
    public abstract class RadzenHtmlEditorColorBase : RadzenHtmlEditorButtonBase
    {
        /// <summary>
        /// Sets <see cref="RadzenColorPicker.ShowHSV" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public bool ShowHSV { get; set; } = true;

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.ShowRGBA" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public bool ShowRGBA { get; set; } = true;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.ShowColors" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public bool ShowColors { get; set; } = true;

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.ShowButton" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public bool ShowButton { get; set; } = true;

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.HexText" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public string HexText { get; set; } = "Hex";

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.RedText" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public string RedText { get; set; } = "R";

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.GreenText" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public string GreenText { get; set; } = "G";

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.BlueText" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public string BlueText { get; set; } = "B";

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.AlphaText" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public string AlphaText { get; set; } = "A";

        /// <summary>
        /// Sets <see cref="RadzenColorPicker.ButtonText" /> of the built-in RadzenColorPicker.
        /// </summary>
        [Parameter]
        public string ButtonText { get; set; } = "OK";


        /// <summary>
        /// Handles the change event of built-in RadzenColorPicker.
        /// </summary>
        /// <param name="value">The new color.</param>
        protected async Task OnChange(string value)
        {
            await Editor.ExecuteCommandAsync(CommandName, value);
        }

        /// <summary>
        /// The default value of the color picker.
        /// </summary>
        public abstract string Value { get; set; }

        /// <summary>
        /// The internal state of the component.
        /// </summary>
        protected string value;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            value = Value;

            base.OnInitialized();
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var valueChanged = parameters.DidParameterChange(nameof(Value), Value);

            await base.SetParametersAsync(parameters);

            if (valueChanged)
            {
                value = Value;
            }
        }
    }
}
