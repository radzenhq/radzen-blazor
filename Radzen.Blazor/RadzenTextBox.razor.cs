using System.Linq;
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
    public partial class RadzenTextBox : FormComponentWithAutoComplete<string>
    {
        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

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
        /// Handles change event of the built-in <c>input</c> elementt.
        /// </summary>
        protected async Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

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

        /// <inheritdoc />
        protected override string GetId()
        {
            return Name ?? base.GetId();
        }
    }
}
