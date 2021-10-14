using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenTemplateForm.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// Implements the <see cref="Radzen.IRadzenForm" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.RadzenComponent" />
    /// <seealso cref="Radzen.IRadzenForm" />
    public class RadzenTemplateForm<TItem> : RadzenComponent, IRadzenForm
    {
        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public TItem Data { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment<EditContext> ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the submit.
        /// </summary>
        /// <value>The submit.</value>
        [Parameter]
        public EventCallback<TItem> Submit { get; set; }

        /// <summary>
        /// Gets or sets the on invalid submit.
        /// </summary>
        /// <value>The on invalid submit.</value>
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

        /// <summary>
        /// Gets or sets the invalid submit.
        /// </summary>
        /// <value>The invalid submit.</value>
        [Parameter]
        public EventCallback<FormInvalidSubmitEventArgs> InvalidSubmit { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>The method.</value>
        [Parameter]
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        [Parameter]
        public string Action { get; set; }

        /// <summary>
        /// The handle submit delegate
        /// </summary>
        private readonly Func<Task> handleSubmitDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RadzenTemplateForm{TItem}"/> class.
        /// </summary>
        public RadzenTemplateForm()
        {
            handleSubmitDelegate = OnSubmit;
        }

        /// <summary>
        /// Called when [submit].
        /// </summary>
        protected async Task OnSubmit()
        {
            if (EditContext != null)
            {
                bool valid = EditContext.Validate();

                if (valid)
                {
                    await Submit.InvokeAsync(Data);

                    if (Action != null)
                    {
                        await JSRuntime.InvokeVoidAsync($"Radzen.submit", Element);
                    }
                }
                else
                {
                    await InvalidSubmit.InvokeAsync(new FormInvalidSubmitEventArgs() { Errors = EditContext.GetValidationMessages() });
                }
            }
            else
            {
                if (Action != null)
                {
                    await JSRuntime.InvokeVoidAsync($"Radzen.submit", Element);
                }
            }
        }

        /// <summary>
        /// The components
        /// </summary>
        List<IRadzenFormComponent> components = new List<IRadzenFormComponent>();

        /// <summary>
        /// Adds the component.
        /// </summary>
        /// <param name="component">The component.</param>
        public void AddComponent(IRadzenFormComponent component)
        {
            if (components.IndexOf(component) == -1)
            {
                components.Add(component);
            }
        }

        /// <summary>
        /// Removes the component.
        /// </summary>
        /// <param name="component">The component.</param>
        public void RemoveComponent(IRadzenFormComponent component)
        {
            components.Remove(component);
        }

        /// <summary>
        /// Finds the component.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>IRadzenFormComponent.</returns>
        public IRadzenFormComponent FindComponent(string name)
        {
            return components.Where(component => component.Name == name).FirstOrDefault();
        }

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        public EditContext EditContext { get; set; }

        /// <summary>
        /// Called when [parameters set].
        /// </summary>
        protected override void OnParametersSet()
        {
            if (Data != null && (EditContext == null || EditContext.Model != (object)Data))
            {
                EditContext = new EditContext(Data);
            }
        }

        /// <summary>
        /// Builds the render tree.
        /// </summary>
        /// <param name="builder">The builder.</param>
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

                builder.AddAttribute(4, "onsubmit", handleSubmitDelegate);
                builder.AddMultipleAttributes(5, Attributes);
                builder.AddAttribute(6, "class", GetCssClass());
                builder.AddElementReferenceCapture(7, form => Element = form);

                builder.OpenComponent<CascadingValue<IRadzenForm>>(8);
                builder.AddAttribute(9, "IsFixed", true);
                builder.AddAttribute(10, "Value", this);
                builder.AddAttribute(11, "ChildContent", new RenderFragment(contentBuilder =>
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