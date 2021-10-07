using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenRating.
    /// Implements the <see cref="Radzen.FormComponent{System.Int32}" />
    /// </summary>
    /// <seealso cref="Radzen.FormComponent{System.Int32}" />
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
        /// Gets or sets the stars.
        /// </summary>
        /// <value>The stars.</value>
        [Parameter]
        public int Stars { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether [read only].
        /// </summary>
        /// <value><c>true</c> if [read only]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="value">The value.</param>
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