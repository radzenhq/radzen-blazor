using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTextBox component.
    /// Implements the <see cref="Radzen.FormComponent{System.String}" />
    /// </summary>
    /// <seealso cref="Radzen.FormComponent{System.String}" />
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
        /// Gets or sets a value indicating whether input automatic complete is enabled.
        /// </summary>
        /// <value><c>true</c> if input automatic complete is enabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AutoComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum length.
        /// </summary>
        /// <value>The maximum length.</value>
        [Parameter]
        public long? MaxLength { get; set; }

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

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textbox").ToString();
        }
    }
}