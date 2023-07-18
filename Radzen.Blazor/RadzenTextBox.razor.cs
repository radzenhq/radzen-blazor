using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// An input component for single line text entry.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTextBox @bind-Value=@value Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenTextBox : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the browser built-in autocomplete is enabled.
        /// </summary>
        /// <value><c>true</c> if input automatic complete is enabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AutoComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating the type of built-in autocomplete
        /// the browser should use.
        /// <see cref="Blazor.AutoCompleteType" />
        /// </summary>
        /// <value>
        /// The type of built-in autocomplete.
        /// </value>
        [Parameter]
        public AutoCompleteType AutoCompleteType { get; set; } = AutoCompleteType.On;

        /// <summary>
        /// Gets or sets the maximum allowed text length.
        /// </summary>
        /// <value>The maximum length.</value>
        [Parameter]
        public long? MaxLength { get; set; }

        /// <summary>
        /// Specifies whether to remove any leading or trailing whitespace from the value. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if trimming is enabled; otherwise, <c>false</c>. </value>
        
        [Parameter]
        public bool Trim { get; set; }
        /// <summary>
        /// Specifies whether the component should use the real-time input event (<c>@oninput</c>) instead of the change event (<c>@onchange</c>) of the <c>input</c> element.
        /// When set to <c>true</c>, the component will handle every real-time change in the <c>input</c> element (e.g., when the user types or deletes text).
        /// When set to <c>false</c>, the component will handle the change event, which is fired when the <c>input</c> element loses focus after its value has changed.
        /// By default, this parameter is set to <c>false</c>, meaning the change event is used.
        /// </summary>

        [Parameter]
        public bool OnUsingOnInput { get; set; } = false;
        /// <summary>
        /// Handles change event of the built-in <c>input</c> elementt.
        /// </summary>
        /// 
        protected async Task OnChange(ChangeEventArgs args)
        {
            if (OnUsingOnInput) return;
            await ChangeText($"{args.Value}");
        }
        /// <summary>
        /// Handles input event of the built-in <c>input</c> elementt.
        /// </summary>
        /// 
        protected async Task OnInput(ChangeEventArgs args)
        {
            if (!OnUsingOnInput) return;

            await ChangeText($"{args.Value}");
        }

        protected async Task ChangeText(string Value)
        {

            if (Trim)
            {
                Value = Value.Trim();
            }

            await ValueChanged.InvokeAsync(Value);

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(Value);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textbox").ToString();
        }

        /// <summary>
        /// Gets the autocomplete attribute's string value.
        /// </summary>
        /// <value>
        /// <c>off</c> if the AutoComplete parameter is false or the
        /// AutoCompleteType parameter is "off". When the AutoComplete
        /// parameter is true, the value is <c>on</c> or, if set, the value of
        /// AutoCompleteType.</value>
        public string AutoCompleteAttribute { get => !AutoComplete ? "off" :
                AutoCompleteType.GetAutoCompleteValue(); }
    }
}
