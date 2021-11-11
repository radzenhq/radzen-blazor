using Microsoft.AspNetCore.Components;

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
    }
}
