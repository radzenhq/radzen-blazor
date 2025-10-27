using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A star rating input component that allows users to provide ratings by selecting a number of stars.
    /// RadzenRating displays an interactive or read-only star rating with configurable number of stars and keyboard accessibility.
    /// Displays a row of stars (or other symbols) that users can click to select a rating value. The value is an integer from 0 to the number of stars configured.
    /// Common uses include product reviews and ratings, user feedback and satisfaction surveys, content quality indicators, and service ratings.
    /// Supports keyboard navigation (arrow keys, Space/Enter) for accessibility. Use ReadOnly mode to display ratings without allowing user input.
    /// </summary>
    /// <example>
    /// Basic 5-star rating:
    /// <code>
    /// &lt;RadzenRating @bind-Value=@rating /&gt;
    /// </code>
    /// Custom number of stars with change event:
    /// <code>
    /// &lt;RadzenRating @bind-Value=@userRating Stars="10" Change=@(args => Console.WriteLine($"Rated: {args} out of 10")) /&gt;
    /// </code>
    /// Read-only rating display:
    /// <code>
    /// &lt;RadzenRating Value=@product.AverageRating Stars="5" ReadOnly="true" /&gt;
    /// &lt;RadzenText&gt;@product.AverageRating out of 5 stars&lt;/RadzenText&gt;
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
        /// Gets or sets the total number of stars to display in the rating component.
        /// The value can range from 0 to this number. Common values are 5 or 10.
        /// </summary>
        /// <value>The total number of stars. Default is 5.</value>
        [Parameter]
        public int Stars { get; set; } = 5;

        /// <summary>
        /// Gets or sets the accessible label text for the clear rating action.
        /// Used by screen readers to announce the clear/reset rating button functionality.
        /// </summary>
        /// <value>The ARIA label for clearing the rating. Default is "Clear".</value>
        [Parameter]
        public string ClearAriaLabel { get; set; } = "Clear";

        /// <summary>
        /// Gets or sets the accessible label text template for rating actions.
        /// Used by screen readers to announce each star's rating value (e.g., "Rate 3 stars").
        /// </summary>
        /// <value>The ARIA label for rating actions. Default is "Rate".</value>
        [Parameter]
        public string RateAriaLabel { get; set; } = "Rate";

        /// <summary>
        /// Gets or sets whether the rating is read-only and cannot be changed by user interaction.
        /// When true, the stars display the current rating but cannot be clicked or modified.
        /// Useful for displaying ratings without allowing users to change them (e.g., showing product ratings).
        /// </summary>
        /// <value><c>true</c> if the rating is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
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
