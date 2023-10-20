using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class of Radzen validator components.
    /// </summary>
    public abstract class ValidatorBase : RadzenComponent, IRadzenFormValidator
    {
        /// <summary>
        /// Gets or sets the form which contains this validator.
        /// </summary>
        [CascadingParameter]
        public IRadzenForm Form { get; set; }

        /// <summary>
        /// Specifies the component which this validator should validate. Must be set to the <see cref="IRadzenFormComponent.Name" /> of an existing component.
        /// </summary>
        [Parameter]
        public string Component { get; set; }

        /// <summary>
        /// Specifies the message displayed when the validator is invalid.
        /// </summary>
        [Parameter]
        public abstract string Text { get; set; }

        /// <summary>
        /// Determines if the validator is displayed as a popup or not. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool Popup { get; set; }

        /// <summary>
        /// Returns the validity status.
        /// </summary>
        /// <value><c>true</c> if this validator is valid; otherwise, <c>false</c>.</value>
        public bool IsValid { get; protected set; } = true;

        /// <summary>
        /// Provided by the <see cref="RadzenTemplateForm{TItem}" /> which contains this validator. Used internally.
        /// </summary>
        /// <value>The edit context.</value>
        [CascadingParameter]
        public EditContext EditContext { get; set; }

        /// <summary>
        /// Stores the validation messages.
        /// </summary>
        protected ValidationMessageStore messages;
        private FieldIdentifier FieldIdentifier { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (Visible)
            {
                if (EditContext != null && messages == null)
                {
                    messages = new ValidationMessageStore(EditContext);
                    Unsubscribe();
                    EditContext.OnFieldChanged += ValidateField;
                    EditContext.OnValidationRequested += ValidateModel;
                    EditContext.OnValidationStateChanged += ValidationStateChanged;
                }
            }
            else
            {
                RemoveFromEditContext();
            }
        }

        void Unsubscribe()
        {
            EditContext.OnFieldChanged -= ValidateField;
            EditContext.OnValidationRequested -= ValidateModel;
            EditContext.OnValidationStateChanged -= ValidationStateChanged;
        }

        void RemoveFromEditContext()
        {
            if (EditContext != null && messages != null)
            {
                Unsubscribe();

                if (FieldIdentifier.FieldName != null)
                {
                    messages.Clear(FieldIdentifier);
                }
            }

            messages = null;
            IsValid = true;
        }

        private void ValidateField(object sender, FieldChangedEventArgs args)
        {
            var component = Form.FindComponent(Component);
            if (component != null)
            {
                if (args.FieldIdentifier.Equals(component.FieldIdentifier))
                {
                    ValidateModel(sender, ValidationRequestedEventArgs.Empty);
                }
            }
        }

        private void ValidateModel(object sender, ValidationRequestedEventArgs args)
        {
            var component = Form.FindComponent(Component);

            if (component == null)
            {
                throw new InvalidOperationException($"Cannot find component with Name {Component}");
            }

            if (component.FieldIdentifier.FieldName != null)
            {
                IsValid = Validate(component);

                messages.Clear(component.FieldIdentifier);

                if (!IsValid)
                {
                    messages.Add(component.FieldIdentifier, Text);
                }

                EditContext?.NotifyValidationStateChanged();
            }

            FieldIdentifier = component.FieldIdentifier;
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-message rz-messages-error {(Popup ? "rz-message-popup" : "")}";
        }

        /// <summary>
        /// Runs validation against the specified component.
        /// </summary>
        /// <param name="component">The component to validate.</param>
        /// <returns><c>true</c> if validation is successful, <c>false</c> otherwise.</returns>
        protected abstract bool Validate(IRadzenFormComponent component);

        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            RemoveFromEditContext();
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Visible && !IsValid)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", Style);
                builder.AddAttribute(2, "class", GetCssClass());
                builder.AddMultipleAttributes(3, Attributes);
                builder.AddContent(4, Text);
                builder.CloseElement();
            }
        }
    }
}
