using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component that validates form inputs using Data Annotations attributes defined on model properties.
    /// RadzenDataAnnotationValidator enables automatic validation based on attributes like [Required], [StringLength], [Range], [EmailAddress], etc.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}"/>.
    /// Uses the standard .NET validation attributes from System.ComponentModel.DataAnnotations. Reads all validation attributes on a model property and validates the input accordingly.
    /// Benefits include centralized validation (define rules once on the model, use everywhere), support for multiple validation attributes per property,
    /// built-in attributes (Required, StringLength, Range, EmailAddress, Phone, Url, RegularExpression, etc.), works with custom ValidationAttribute implementations,
    /// and multiple errors joined with MessageSeparator.
    /// Ideal when your validation rules are already defined on your data models using data annotations. Automatically extracts error messages from the attributes' ErrorMessage properties.
    /// </summary>
    /// <example>
    /// Model-based validation with data annotations:
    /// <code>
    /// &lt;RadzenTemplateForm TItem="UserModel" Data=@user&gt;
    ///     &lt;RadzenTextBox Name="Email" @bind-Value=@user.Email /&gt;
    ///     &lt;RadzenDataAnnotationValidator Component="Email" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///     class UserModel
    ///     {
    ///         [Required(ErrorMessage = "Email is required")]
    ///         [EmailAddress(ErrorMessage = "Invalid email format")]
    ///         [StringLength(100, ErrorMessage = "Email too long")]
    ///         public string Email { get; set; }
    ///     }
    ///     UserModel user = new UserModel();
    /// }
    /// </code>
    /// Custom error separator:
    /// <code>
    /// &lt;RadzenDataAnnotationValidator Component="Name" MessageSeparator=" | " /&gt;
    /// </code>
    /// </example>
    public class RadzenDataAnnotationValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the validation error message.
        /// This property is automatically populated with error messages from data annotation attributes when validation fails.
        /// If multiple attributes fail, messages are joined using <see cref="MessageSeparator"/>.
        /// </summary>
        /// <value>The validation error message(s).</value>
        [Parameter]
        public override string Text { get; set; }

        /// <summary>
        /// Gets or sets the text used to join multiple validation error messages.
        /// When multiple data annotation attributes fail (e.g., both Required and StringLength), their messages are combined with this separator.
        /// </summary>
        /// <value>The message separator text. Default is " and ".</value>
        [Parameter]
        public string MessageSeparator { get; set; } = " and ";

        /// <summary>
        /// Service provider injected from the Dependency Injection (DI) container.
        /// </summary>
        [Inject]
        public IServiceProvider ServiceProvider { get; set; }

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            var validationResults = new List<ValidationResult>();

            var model = component.FieldIdentifier.Model;

            var getter = PropertyAccess.Getter<object>(model, component.FieldIdentifier.FieldName);

            var value = getter(model);

            var validationContext = new ValidationContext(model, ServiceProvider, null)
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

