using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen.Blazor
{
    public abstract class ValidatorBase : RadzenComponent, IRadzenFormValidator
    {
        [CascadingParameter]
        public IRadzenForm Form { get; set; }

        [Parameter]
        public string Component { get; set; }

        [Parameter]
        public abstract string Text { get; set; }

        [Parameter]
        public bool Popup { get; set; }

        public bool IsValid { get; protected set; } = true;

        [CascadingParameter]
        public EditContext EditContext { get; set; }

        protected ValidationMessageStore messages;

        private FieldIdentifier FieldIdentifier { get; set; }

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

        private void ValidateField(object sender, FieldChangedEventArgs args)
        {
            var component = Form.FindComponent(Component);

            if (args.FieldIdentifier.FieldName == component?.FieldIdentifier.FieldName)
            {
                ValidateModel(sender, ValidationRequestedEventArgs.Empty);
            }
        }

        private void ValidateModel(object sender, ValidationRequestedEventArgs args)
        {
            var component = Form.FindComponent(Component);

            if (component == null)
            {
                throw new InvalidOperationException($"Cannot find component with Name {Component}");
            }

            IsValid = Validate(component);

            messages.Clear(component.FieldIdentifier);

            if (!IsValid)
            {
                messages.Add(component.FieldIdentifier, Text);
            }

            EditContext?.NotifyValidationStateChanged();

            FieldIdentifier = component.FieldIdentifier;
        }

        protected override string GetComponentCssClass()
        {
            return $"rz-message rz-messages-error {(Popup ? "rz-message-popup" : "")}";
        }

        protected abstract bool Validate(IRadzenFormComponent component);

        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        public override void Dispose()
        {
            base.Dispose();

            RemoveFromEditContext();
        }

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