using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRating component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenRating Stars="10" Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenSecurityCode : FormComponent<string>
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-security-code").ToString();
        }

        /// <summary>
        /// Gets or sets the number of input.
        /// </summary>
        /// <value>The number of input.</value>
        [Parameter]
        public int Count { get; set; } = 4;

        /// <summary>
        /// Gets or sets the number of input.
        /// </summary>
        /// <value>The number of input.</value>
        [Parameter]
        public SecurityCodeType Type { get; set; } = SecurityCodeType.String;

        /// <summary>
        /// Gets or sets the spacing between inputs
        /// </summary>
        /// <value>The spacing between inputs.</value>
        [Parameter]
        public string? Gap { get; set; }

        private string? ariaLabel;

        /// <summary>
        /// Gets or sets the accessible label text of the security code group.
        /// </summary>
        /// <value>The ARIA label of the security code group. Default is "Security code".</value>
        [Parameter]
        public string AriaLabel { get => ariaLabel ?? Localize(nameof(RadzenStrings.SecurityCode_AriaLabel)); set => ariaLabel = value; }

        private string? inputAriaLabelFormat;

        /// <summary>
        /// Gets or sets the format string used to build the accessible label of each input.
        /// The first argument is the input position and the second one is the total number of inputs.
        /// </summary>
        /// <value>The ARIA label format of each input. Default is "Character {0} of {1}".</value>
        [Parameter]
        public string InputAriaLabelFormat { get => inputAriaLabelFormat ?? Localize(nameof(RadzenStrings.SecurityCode_InputAriaLabelFormat)); set => inputAriaLabelFormat = value; }

        string GetInputAriaLabel(int index)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, InputAriaLabelFormat, index, Count);
        }

        IJSObjectReference? _jsRef;

        bool firstRender;
        bool visibleChanged;
        bool disabledChanged;
        bool countChanged;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);
            disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);
            countChanged = parameters.DidParameterChange(nameof(Count), Count);

            await base.SetParametersAsync(parameters);

            if ((visibleChanged || disabledChanged) && !firstRender)
            {
                if (!Visible || !Disabled)
                {
                    Dispose();
                }
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged || disabledChanged || countChanged)
            {
                if (visibleChanged)
                {
                    visibleChanged = false;
                }

                if (disabledChanged)
                {
                    disabledChanged = false;
                }

                if (countChanged)
                {
                    countChanged = false;
                }

                if (Visible && !Disabled && JSRuntime != null)
                {
                    if (_jsRef != null)
                    {
                        await _jsRef.InvokeVoidAsync("dispose");
                        await _jsRef.DisposeAsync();
                    }

                    _jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createSecurityCode", GetId(), Reference, Element,
                        Type == SecurityCodeType.Numeric ? true : false);

                    StateHasChanged();
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            _jsRef?.InvokeVoidAsync("dispose");
            _jsRef?.DisposeAsync();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called when value changed.
        /// </summary>
        /// <param name="value">The value.</param>
        [JSInvokable("RadzenSecurityCode.OnValueChange")]
        public async Task OnValueChange(string value)
        {
            await ValueChanged.InvokeAsync(value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(value);
        }

        string ElementAt(int index)
        {
            var ch = $"{Value}".ElementAtOrDefault(index);

            if (Type == SecurityCodeType.Numeric && !Char.IsNumber(ch))
            {
                return " ";
            }

            return ch != default(char) ? ch.ToString() : " ";
        }

        /// <inheritdoc/>
        public override async ValueTask FocusAsync()
        {
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.focusSecurityCode", Element);
            }
        }
    }
}
