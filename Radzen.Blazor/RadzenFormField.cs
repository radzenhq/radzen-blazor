using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    public interface IFormFieldContext
    {
        Action<bool> DisabledChanged { get; set; }
    }

    public class FormFieldContext : IFormFieldContext
    {
        public Action<bool> DisabledChanged { get; set; }
    }

    public partial class RadzenFormField : RadzenComponent
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Start { get; set; }

        [Parameter]
        public RenderFragment End { get; set; }

        [Parameter]
        public RenderFragment Helper { get; set; }

        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the design variant of the form field.
        /// </summary>
        /// <value>The variant of the form field.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Outlined;

        private bool disabled;

        private readonly IFormFieldContext context;

        /// constructor
        public RadzenFormField()
        {
            context = new FormFieldContext { DisabledChanged = DisabledChanged };
        }

        private void DisabledChanged(bool value)
        {
            disabled = value;
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create($"rz-form-field rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()}").AddDisabled(disabled).ToString();
        }
    }
}