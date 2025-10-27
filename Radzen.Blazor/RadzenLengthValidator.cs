using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component that ensures a text input's length falls within a specified minimum and maximum range.
    /// RadzenLengthValidator is useful for enforcing username lengths, password complexity, or limiting text field sizes.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}"/> and associated with a named input component.
    /// Checks the string length against optional Min and Max constraints.
    /// If only Min is set, validates minimum length. If only Max is set, validates maximum length. If both are set, the length must be within the range (inclusive).
    /// Null or empty values are considered invalid if Min is set, and valid if only Max is set.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///    &lt;RadzenTextBox style="display: block" Name="FirstName" @bind-Value=@model.FirstName /&gt;
    ///    &lt;RadzenLengthValidator Component="FirstName" Min="3" Text="First name should be at least 3 characters" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///    class Model
    ///    {
    ///       public string FirstName { get; set; }
    ///    }
    ///    Model model = new Model(); 
    /// }
    /// </code>
    /// </example>
    public class RadzenLengthValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the message displayed when the component is invalid. Set to <c>"Invalid length"</c> by default.
        /// </summary>
        [Parameter]
        public override string Text { get; set; } = "Invalid length";

        /// <summary>
        /// Specifies the minimum accepted length. The component value length should be greater than the minimum in order to be valid.
        /// </summary>
        [Parameter]
        public int? Min { get; set; }

        /// <summary>
        /// Specifies the maximum accepted length. The component value length should be less than the maximum in order to be valid.
        /// </summary>
        [Parameter]
        public int? Max { get; set; }

        /// <inheritdoc />
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