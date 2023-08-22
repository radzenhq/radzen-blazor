using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component which compares a component value with a specified value.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///    &lt;RadzenTextBox Name="Email" @bind-Value=@model.Email /&gt;
    ///    &lt;RadzenCustomValidator Value=@model.Email Component="Email" Text="Email must be unique" Validator="@(() => ValidateNewEmail(model.Email))" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///    class Model
    ///    {
    ///         public string Email { get; set; }
    ///    }
    ///    Model model = new Model();
    ///
    ///    string[] emails = new string[] { "andy@smith" };
    ///
    ///    bool ValidateNewEmail(string email)
    ///    {
    ///        return !emails.Any(e => e.ToUpper().Equals(email.ToUpper()));
    ///    }
    /// }
    /// </code>
    /// </example>
    public class RadzenCustomValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the message displayed when the component is invalid. Set to <c>"Value should match"</c> by default.
        /// </summary>
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        /// <summary>
        /// Specifies the function which validates the component value. Must return <c>true</c> if the component is valid.
        /// </summary>
        [Parameter]
        public Func<bool> Validator { get; set; } = () => true;

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            return Validator();
        }
    }
}