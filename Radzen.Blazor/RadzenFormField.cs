using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public interface IFormFieldContext
    {

    }

    class FormFieldContext : IFormFieldContext
    {

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
        public string Text { get; set; }

        [Parameter]
        public string Component { get; set; }

        private IFormFieldContext context = new FormFieldContext();

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-form-field";
        }
    }
}