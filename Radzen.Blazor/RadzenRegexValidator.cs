using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component which matches a component value against a specified regular expression pattern.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///    &lt;RadzenTextBox style="display: block" Name="ZIP" @bind-Value=@model.Zip /&gt;
    ///    &lt;RadzenRegexValidator Component="ZIP" Text="ZIP code must be 5 digits" Pattern="\d{5}" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///    class Model
    ///    {
    ///       public string Zip { get; set; }
    ///    }
    ///    Model model = new Model(); 
    /// }
    /// </code>
    /// </example>
    public class RadzenRegexValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the message displayed when the component is invalid. Set to <c>"Value should match"</c> by default.
        /// </summary>
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        /// <summary>
        /// Specifies the regular expression pattern which the component value should match in order to be valid.
        /// </summary>
        [Parameter]
        public string Pattern { get; set; }

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            return new RegularExpressionAttribute(Pattern).IsValid(component.GetValue());
        }
    }
}