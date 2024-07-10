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
        /// The message is generated from the data annotation attributes applied to the model property.
        /// </summary>
        [Parameter]
        public override string Text { get; set; }

        /// <summary>
        /// Gets or sets the separator used to join multiple validation messages.
        /// </summary>
        [Parameter]
        public string MessageSeparator { get; set; } = " and ";

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            var validationResults = new List<ValidationResult>();

            var getter = PropertyAccess.Getter<object>(EditContext.Model, component.FieldIdentifier.FieldName);

            var value = getter(EditContext.Model);

            var validationContext = new ValidationContext(EditContext.Model)
            {
                MemberName = component.FieldIdentifier.FieldName
            };

            var isValid = Validator.TryValidateProperty(value, validationContext, validationResults);

            if (!isValid)
            {
                Text = string.Join(MessageSeparator, validationResults.Select(vr => vr.ErrorMessage));
            }

            return isValid;
        }
    }
}

