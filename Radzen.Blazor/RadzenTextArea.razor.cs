using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A multi-line text input component for entering longer text content with configurable dimensions.
    /// RadzenTextArea provides a resizable textarea with data binding, validation, and automatic sizing options.
    /// Ideal for comments, descriptions, messages, or any content requiring multiple lines.
    /// Features configurable sizing via Rows (height) and Cols (width) properties, MaxLength to restrict input length,
    /// browser-resizable textarea, integration with Blazor EditContext for form validation, and two-way binding via @bind-Value.
    /// The Rows and Cols properties set the initial/minimum size, but users can often resize the textarea using the resize handle.
    /// </summary>
    /// <example>
    /// Basic textarea:
    /// <code>
    /// &lt;RadzenTextArea @bind-Value=@description Rows="5" Placeholder="Enter description..." /&gt;
    /// </code>
    /// Textarea with character limit:
    /// <code>
    /// &lt;RadzenTextArea @bind-Value=@comment Rows="4" Cols="50" MaxLength="500" Placeholder="Enter your comment (max 500 characters)" /&gt;
    /// </code>
    /// Read-only textarea for display:
    /// <code>
    /// &lt;RadzenTextArea Value=@savedContent Rows="10" ReadOnly="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenTextArea : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets the maximum number of characters that can be entered in the textarea.
        /// When set, the browser prevents users from entering more characters than this limit.
        /// </summary>
        /// <value>The maximum character length, or null for no limit. Default is null.</value>
        [Parameter]
        public long? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets whether the textarea is read-only and cannot be edited by the user.
        /// When true, the textarea displays the value but prevents user input while still allowing selection and copying.
        /// </summary>
        /// <value><c>true</c> if the textarea is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the number of visible text rows (height) in the textarea.
        /// This determines the initial height of the textarea. Users may be able to resize it depending on browser and CSS settings.
        /// </summary>
        /// <value>The number of rows. Default is 2.</value>
        [Parameter]
        public int Rows { get; set; } = 2;

        /// <summary>
        /// Gets or sets the number of visible text columns (width) in the textarea.
        /// This determines the initial width of the textarea based on average character width. 
        /// In modern CSS layouts, setting an explicit width via Style property is often preferred.
        /// </summary>
        /// <value>The number of columns. Default is 20.</value>
        [Parameter]
        public int Cols { get; set; } = 20;

        /// <summary>
        /// Gets or sets a value indicating whether the component should update the value immediately when the user types. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool Immediate { get; set; }

        /// <summary>
        /// Handles the <see cref="E:Change" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected async Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

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
            return GetClassList("rz-textarea").ToString();
        }

        /// <inheritdoc />
        protected override string GetId()
        {
            return Name ?? base.GetId();
        }
    }
}
