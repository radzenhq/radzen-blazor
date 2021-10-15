using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component which checks if a component value is within a specified range.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///    &lt;RadzenNumeric style="display: block" Name="Quantity" @bind-Value=@model.Quantity /&gt;
    ///    &lt;RadzenNumericRangeValidator Component="Quantity" Min="1" Max="10" Text="Quantity should be between 1 and 10" Style="position: absolute" /&gt; 
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///    class Model
    ///    {
    ///       public decimal Quantity { get; set; }
    ///    }
    ///    Model model = new Model(); 
    /// }
    /// </code>
    /// </example>>
    public class RadzenNumericRangeValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the message displayed when the component is invalid. Set to <c>"Not in the valid range"</c> by default.
        /// </summary>
        [Parameter]
        public override string Text { get; set; } = "Not in the valid range";

        /// <summary>
        /// Specifies the minimum value. The component value should be greater than the minimum in order to be valid.
        /// </summary>
        [Parameter]
        public dynamic Min { get; set; }

        /// <summary>
        /// Specifies the maximum value. The component value should be less than the maximum in order to be valid.
        /// </summary>
        [Parameter]
        public dynamic Max { get; set; }

        /// <inheritdoc />
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