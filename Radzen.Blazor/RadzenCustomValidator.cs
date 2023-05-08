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
    ///    &lt;RadzenCustomValidator Value=@model.Email Component="Email" Text="Email must be unique" CheckIsValid="@(ValidateNewEmail(model.Email))" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///    class Model
    ///    {
    ///         public string Email { get; set; }
    ///         public Model() { }
    ///         public Model(string email) :this()
    ///         {
    ///            Email = email;
    ///         }
    ///    } 
    ///    Model model = new Model();
    ///    
    ///    IList<Model> models = new List<Model>()
    ///    {
    ///         new Model("Smith", "andy@smith.com")
    ///    };
    ///    
    ///    bool ValidateNewEmail(string email)
    ///    {
    ///        return models.Where(m => m.Email.ToUpper().Equals(email?.ToUpper())).Count() == 0;
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
        /// Gets or sets the Valid. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool CheckIsValid { get; set; } = false;

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            return CheckIsValid;
        }
    }
}