using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component which checks if a component value is a valid email address.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///   &lt;RadzenTextBox style="display: block" Name="Email" @bind-Value=@model.Email /&gt;
    ///   &lt;RadzenEmailValidator Component="Email" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///  class Model
    ///   {
    ///    public string Email { get; set; }
    ///  }
    ///  
    ///  Model model = new Model();
    /// }
    /// </code>
    /// </example>
    public class RadzenEmailValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the message displayed when the component is invalid. Set to <c>"Invalid email"</c> by default.
        /// </summary>
        [Parameter]
        public override string Text { get; set; } = "Invalid email";

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            var value = component.GetValue();
            var valueAsString = value as string;

            if (string.IsNullOrEmpty(valueAsString))
            {
                return true;
            }

            var email = new EmailAddressAttribute();

            return email.IsValid(valueAsString);
        }
    }
}