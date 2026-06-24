using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A signature pad component that allows users to draw a signature using mouse, touch, or pen input.
    /// The signature is captured as a base64-encoded PNG data URL. Supports data binding, validation, and form integration.
    /// </summary>
    /// <example>
    /// Basic usage with two-way binding:
    /// <code>
    /// &lt;RadzenSignaturePad @bind-Value=@signature /&gt;
    /// </code>
    /// With custom size and stroke:
    /// <code>
    /// &lt;RadzenSignaturePad @bind-Value=@signature Style="width: 500px; height: 200px;" StrokeColor="#000" StrokeWidth="2" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenSignaturePad : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets the stroke color used for drawing.
        /// </summary>
        /// <value>The stroke color. Default is "#000".</value>
        [Parameter]
        public string StrokeColor { get; set; } = "#000";

        /// <summary>
        /// Gets or sets the width of the stroke in pixels.
        /// </summary>
        /// <value>The stroke width. Default is 2.</value>
        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether the signature pad is read-only.
        /// </summary>
        /// <value><c>true</c> if read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user can clear the signature. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if clearing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClear { get; set; } = true;

        private string? clearAriaLabel;

        /// <summary>
        /// Gets or sets the accessible label text for the clear button.
        /// </summary>
        /// <value>The ARIA label for clearing the signature. Default is "Clear".</value>
        [Parameter]
        public string ClearAriaLabel { get => clearAriaLabel ?? Localize(nameof(RadzenStrings.SignaturePad_ClearAriaLabel)); set => clearAriaLabel = value; }

        IJSObjectReference? _jsRef;
        bool _needsUpdate;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList = GetClassList("rz-signature-pad");

            if (Disabled)
            {
                classList.Add("rz-state-disabled");
            }

            if (ReadOnly)
            {
                classList.Add("rz-state-readonly");
            }

            return classList.ToString();
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var strokeColorChanged = parameters.DidParameterChange(nameof(StrokeColor), StrokeColor);
            var strokeWidthChanged = parameters.DidParameterChange(nameof(StrokeWidth), StrokeWidth);
            var disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);
            var readOnlyChanged = parameters.DidParameterChange(nameof(ReadOnly), ReadOnly);

            await base.SetParametersAsync(parameters);

            if (strokeColorChanged || strokeWidthChanged || disabledChanged || readOnlyChanged)
            {
                _needsUpdate = true;
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                if (Visible && JSRuntime != null)
                {
                    _jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>(
                        "Radzen.createSignaturePad", Element, Reference, StrokeColor, StrokeWidth, Disabled || ReadOnly, Value);
                }
            }
            else if (_needsUpdate && _jsRef != null)
            {
                _needsUpdate = false;
                await _jsRef.InvokeVoidAsync("update", StrokeColor, StrokeWidth, Disabled || ReadOnly);
            }
        }

        /// <summary>
        /// Called from JavaScript when the user finishes drawing a stroke.
        /// </summary>
        /// <param name="dataUrl">The base64-encoded PNG data URL of the signature.</param>
        [JSInvokable("RadzenSignaturePad.OnStrokeEnd")]
        public async Task OnStrokeEnd(string dataUrl)
        {
            Value = dataUrl;

            await ValueChanged.InvokeAsync(Value);

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        /// <summary>
        /// Clears the signature pad.
        /// </summary>
        public async Task Clear()
        {
            if (_jsRef != null)
            {
                await _jsRef.InvokeVoidAsync("clear");
            }

            Value = null;

            await ValueChanged.InvokeAsync(Value);

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(Value);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            _jsRef?.InvokeVoidAsync("dispose");
            _jsRef?.DisposeAsync();

            GC.SuppressFinalize(this);
        }
    }
}
