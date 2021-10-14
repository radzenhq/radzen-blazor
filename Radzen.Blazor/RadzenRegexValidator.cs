using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRegexValidator component.
    /// Implements the <see cref="Radzen.Blazor.ValidatorBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.ValidatorBase" />
    public class RadzenRegexValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        /// <summary>
        /// Gets or sets the pattern.
        /// </summary>
        /// <value>The pattern.</value>
        [Parameter]
        public string Pattern { get; set; }

        /// <summary>
        /// Validates the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool Validate(IRadzenFormComponent component)
        {
            return new RegularExpressionAttribute(Pattern).IsValid(component.GetValue());
        }
    }
}