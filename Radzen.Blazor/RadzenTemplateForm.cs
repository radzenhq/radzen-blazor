using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;

namespace Radzen.Blazor
{
    public class RadzenTemplateForm<TItem> : RadzenComponent, IRadzenForm
    {
        public bool IsValid
        {
            get
            {
                if (EditContext == null)
                {
                    return true;
                }

                return !EditContext.GetValidationMessages().Any();
            }
        }

        [Parameter]
        public TItem Data { get; set; }

        [Parameter]
        public RenderFragment<EditContext> ChildContent { get; set; }

        [Parameter]
        public EventCallback<TItem> Submit { get; set; }

        [Parameter]
        [Obsolete]
        public EventCallback<FormInvalidSubmitEventArgs> OnInvalidSubmit
        {
            get
            {
                return InvalidSubmit;
            }
            set
            {
                InvalidSubmit = value;
            }
        }

        [Parameter]
        public EventCallback<FormInvalidSubmitEventArgs> InvalidSubmit { get; set; }

        [Parameter]
        public string Method { get; set; }

        [Parameter]
        public string Action { get; set; }

        private readonly Func<Task> handleSubmitDelegate;

        public RadzenTemplateForm()
        {
            handleSubmitDelegate = OnSubmit;
        }

        protected async Task OnSubmit()
        {
            if (EditContext != null)
            {
                bool valid = false;

                try
                {
                    valid = EditContext.Validate();
                }
                catch
                {

                }

                if (valid)
                {
                    await Submit.InvokeAsync(Data);
                }
                else
                {
                    await InvalidSubmit.InvokeAsync(new FormInvalidSubmitEventArgs() { Errors = EditContext.GetValidationMessages() });
                }

            }
        }

        List<IRadzenFormComponent> components = new List<IRadzenFormComponent>();

        public void AddComponent(IRadzenFormComponent component)
        {
            if (components.IndexOf(component) == -1)
            {
                components.Add(component);
            }
        }

        public void RemoveComponent(IRadzenFormComponent component)
        {
            components.Remove(component);
        }

        public IRadzenFormComponent FindComponent(string name)
        {
            return components.Where(component => component.Name == name).FirstOrDefault();
        }

        public EditContext EditContext { get; set; }

        protected override void OnParametersSet()
        {
            if (Data != null && (EditContext == null || EditContext.Model != (object)Data))
            {
                EditContext = new EditContext(Data);
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Visible)
            {
                if (Data != null)
                {
                    builder.OpenRegion(Data.GetHashCode());
                }

                builder.OpenElement(0, "form");
                builder.AddAttribute(1, "style", Style);

                if (Action != null)
                {
                    builder.AddAttribute(2, "method", Method);
                    builder.AddAttribute(3, "action", Action);
                }
                else
                {
                    builder.AddAttribute(4, "onsubmit", handleSubmitDelegate);
                }

                builder.AddMultipleAttributes(5, Attributes);
                builder.AddAttribute(6, "class", GetCssClass());

                builder.OpenComponent<CascadingValue<IRadzenForm>>(7);
                builder.AddAttribute(8, "IsFixed", true);
                builder.AddAttribute(9, "Value", this);
                builder.AddAttribute(10, "ChildContent", new RenderFragment(contentBuilder =>
                {
                    contentBuilder.OpenComponent<CascadingValue<EditContext>>(0);
                    contentBuilder.AddAttribute(1, "IsFixed", true);
                    contentBuilder.AddAttribute(2, "Value", EditContext);
                    contentBuilder.AddAttribute(3, "ChildContent", ChildContent?.Invoke(EditContext));
                    contentBuilder.CloseComponent();
                }));

                builder.CloseComponent(); // CascadingValue<IRadzenForm>

                builder.CloseElement(); // form

                if (Data != null)
                {
                    builder.CloseRegion();
                }
            }
        }
    }
}