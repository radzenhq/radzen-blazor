using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component which validates a component value using the data annotations
    /// defined on the corresponding model property.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="UserModel" Data=@user&gt;
    ///    &lt;RadzenTextBox style="display: block" Name="Name" @bind-Value=@user.Name /&gt;
    ///    &lt;RadzenFieldValidator Component="Name" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///    class UserModel
    ///    {
    ///       [Required(ErrorMessage = "Name is required.")]
    ///       [StringLength(50, ErrorMessage = "Name must be less than 50 characters.")]
    ///       public string Name { get; set; }
    ///    }
    ///    UserModel user = new UserModel(); 
    /// }
    /// </code>
    /// </example>
    public class RadzenDataAnnotationValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the message displayed when the component is invalid.
        /// The message is generated from the data annotations applied to the model property.
        /// </summary>
        [Parameter]
        public override string Text { get; set; }

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            var editContext = EditContext;
            var fieldIdentifier = component.FieldIdentifier;
            var validationResults = new List<ValidationResult>();

            // Try to get the value of the field
            var field = editContext.Model.GetType().GetProperty(fieldIdentifier.FieldName);
            if (field == null)
            {
                throw new InvalidOperationException($"Field '{fieldIdentifier.FieldName}' not found on model '{editContext.Model.GetType().Name}'.");
            }

            var fieldValue = field.GetValue(editContext.Model);

            // Validate the field using data annotations
            var validationContext = new ValidationContext(editContext.Model)
            {
                MemberName = fieldIdentifier.FieldName
            };

            var isValid = Validator.TryValidateProperty(fieldValue, validationContext, validationResults);
            if (!isValid)
            {
                Text = string.Join(" and ", validationResults.Select(vr => vr.ErrorMessage));
            }

            return isValid;
        }
    }
}

