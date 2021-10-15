using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRating component.
    /// Implements the <see cref="Radzen.FormComponent{System.Int32}" />
    /// </summary>
    /// <seealso cref="Radzen.FormComponent{System.Int32}" />
    /// <example>
    /// <code>
    /// &lt;RadzenRating Stars="10" Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenRating : FormComponent<int>
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-rating").Add("rz-state-readonly", ReadOnly).ToString();
        }

        /// <summary>
        /// Gets or sets the number of stars.
        /// </summary>
        /// <value>The number of stars.</value>
        [Parameter]
        public int Stars { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        private async System.Threading.Tasks.Task SetValue(int value)
        {
            if (!Disabled && !ReadOnly)
            {
                Value = value;

                await ValueChanged.InvokeAsync(value);
                if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
                await Change.InvokeAsync(value);
            }
        }
    }
}