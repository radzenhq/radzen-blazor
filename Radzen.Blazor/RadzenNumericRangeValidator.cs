using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenNumericRangeValidator.
    /// Implements the <see cref="Radzen.Blazor.ValidatorBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.ValidatorBase" />
    public class RadzenNumericRangeValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Not in the valid range";

        /// <summary>
        /// Determines the minimum of the parameters.
        /// </summary>
        /// <value>The minimum.</value>
        [Parameter]
        public dynamic Min { get; set; }

        /// <summary>
        /// Determines the maximum of the parameters.
        /// </summary>
        /// <value>The maximum.</value>
        [Parameter]
        public dynamic Max { get; set; }
        /// <summary>
        /// Validates the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool Validate(IRadzenFormComponent component)
        {
            dynamic value = component.GetValue();

            if (Min != null && ((value != null && value < Min) || value == null))
            {
                return false;
            }

            if (Max != null && (value != null && value > Max))
            {
                return false;
            }

            return true;
        }
    }
}