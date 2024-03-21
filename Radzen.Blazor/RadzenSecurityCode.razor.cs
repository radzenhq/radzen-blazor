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
        public string Gap { get; set; }

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

                if (Visible && !Disabled)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.createSecurityCode", GetId(), Reference, Element,
                        Type == SecurityCodeType.Numeric ? true : false);

                    StateHasChanged();
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroySecurityCode", GetId(), Element);
            }
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

#if NET5_0_OR_GREATER
        /// <inheritdoc/>
        public override async ValueTask FocusAsync()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.focusSecurityCode", Element);
        }
#endif
    }
}
