using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
    public partial class RadzenRating : FormComponent<int>
    {
        /// <inheritdoc />
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
        /// Gets or sets the clear aria label text.
        /// </summary>
        /// <value>The clear aria label text.</value>
        [Parameter]
        public string ClearAriaLabel { get; set; } = "Clear";

        /// <summary>
        /// Gets or sets the rate aria label text.
        /// </summary>
        /// <value>The rate aria label text.</value>
        [Parameter]
        public string RateAriaLabel { get; set; } = "Rate";

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

        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args, Task task)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                await task;
            }
            else
            {
                preventKeyPress = false;
            }
        }
    }
}
