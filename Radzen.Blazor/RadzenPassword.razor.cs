using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPassword component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenPassword Placeholder="Enter password..." Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenPassword : FormComponent<string>, IRadzenFormComponent
    {
        private string InputType = "password";

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether input automatic complete is allowed.
        /// </summary>
        /// <value><c>true</c> if input automatic complete is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AutoComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show Eye icon in password input
        /// </summary>
        /// <value><c>true</c> if password reveal icon is allowed otherwise, <c>fasle</c>.</value>
        [Parameter]
        public bool ShowPasswordReveal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable Password Reveal if the Input is disabled
        /// </summary>
        /// <value><c>true</c> if password reveal icon is allowed and the input is disabled otherwise, <c>fasle</c>.</value>
        [Parameter]
        public bool AllowRevealWhenDisabled { get; set; }

        /// <summary>
        /// Gets or sets the input CSS class.
        /// </summary>
        /// <value>The input CSS class.</value>
        [Parameter]
        public string InputClass { get; set; }

        /// <summary>
        /// Gets input reference.
        /// </summary>
        protected ElementReference input;

        /// <summary>
        /// Handles the <see cref="E:Change" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textbox").ToString();
        }

        /// <summary>
        /// Handles the Show Password toggle
        /// </summary>
        protected System.Threading.Tasks.Task OnToggle()
        {
            InputType = InputType == "password" ? "text" : "password";
            return Task.CompletedTask;
        }

        private string getStyle()
        {
            return $"display: inline-block; border:none; margin:0; padding:0; width:100%;{(Style != null ? Style : "")}";
        }

    }
}
