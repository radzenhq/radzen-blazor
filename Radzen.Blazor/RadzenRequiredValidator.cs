using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRequiredValidator component.
    /// Implements the <see cref="Radzen.Blazor.ValidatorBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.ValidatorBase" />
    public class RadzenRequiredValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Required";

        /// <summary>
        /// Validates the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool Validate(IRadzenFormComponent component)
        {
            return component.HasValue && !object.Equals(DefaultValue, component.GetValue());
        }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        [Parameter]
        public object DefaultValue { get; set; }
    }
}