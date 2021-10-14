using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenLengthValidator component.
    /// Implements the <see cref="Radzen.Blazor.ValidatorBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.ValidatorBase" />
    public class RadzenLengthValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Invalid length";

        /// <summary>
        /// Determines the minimum value.
        /// </summary>
        /// <value>The minimum.</value>
        [Parameter]
        public int? Min { get; set; }

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum.</value>
        [Parameter]
        public int? Max { get; set; }

        /// <summary>
        /// Validates the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool Validate(IRadzenFormComponent component)
        {
            string value = component.GetValue() as string;

            if (Min.HasValue && ((value != null && value.Length < Min) || value == null))
            {
                return false;
            }

            if (Max.HasValue && (value != null && value.Length > Max))
            {
                return false;
            }

            return true;
        }
    }
}