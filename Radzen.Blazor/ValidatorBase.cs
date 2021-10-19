using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class ValidatorBase.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// Implements the <see cref="Radzen.IRadzenFormValidator" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    /// <seealso cref="Radzen.IRadzenFormValidator" />
    public abstract class ValidatorBase : RadzenComponent, IRadzenFormValidator
    {
        /// <summary>
        /// Gets or sets the form.
        /// </summary>
        /// <value>The form.</value>
        [CascadingParameter]
        public IRadzenForm Form { get; set; }

        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        /// <value>The component.</value>
        [Parameter]
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public abstract string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValidatorBase"/> is popup.
        /// </summary>
        /// <value><c>true</c> if popup; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Popup { get; set; }

        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid { get; protected set; } = true;

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        [CascadingParameter]
        public EditContext EditContext { get; set; }

        /// <summary>
        /// The messages
        /// </summary>
        protected ValidationMessageStore messages;

        /// <summary>
        /// Gets or sets the field identifier.
        /// </summary>
        /// <value>The field identifier.</value>
        private FieldIdentifier FieldIdentifier { get; set; }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (Visible)
            {
                if (EditContext != null && messages == null)
                {
                    messages = new ValidationMessageStore(EditContext);
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

        /// <summary>
        /// Removes from edit context.
        /// </summary>
        void RemoveFromEditContext()
        {
            if (EditContext != null && messages != null)
            {
                EditContext.OnFieldChanged -= ValidateField;
                EditContext.OnValidationRequested -= ValidateModel;
                EditContext.OnValidationStateChanged -= ValidationStateChanged;

                if (FieldIdentifier.FieldName != null)
                {
                    messages.Clear(FieldIdentifier);
                }
            }

            messages = null;
            IsValid = true;
        }

        /// <summary>
        /// Validates the field.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="FieldChangedEventArgs"/> instance containing the event data.</param>
        private void ValidateField(object sender, FieldChangedEventArgs args)
        {
            var component = Form.FindComponent(Component);

            if (args.FieldIdentifier.FieldName == component?.FieldIdentifier.FieldName)
            {
                ValidateModel(sender, ValidationRequestedEventArgs.Empty);
            }
        }

        /// <summary>
        /// Validates the model.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ValidationRequestedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="InvalidOperationException">Cannot find component with Name {Component}</exception>
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

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return $"rz-message rz-messages-error {(Popup ? "rz-message-popup" : "")}";
        }

        /// <summary>
        /// Validates the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected abstract bool Validate(IRadzenFormComponent component);

        /// <summary>
        /// Validations the state changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ValidationStateChangedEventArgs"/> instance containing the event data.</param>
        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            RemoveFromEditContext();
        }

        /// <summary>
        /// Builds the render tree.
        /// </summary>
        /// <param name="builder">The builder.</param>
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