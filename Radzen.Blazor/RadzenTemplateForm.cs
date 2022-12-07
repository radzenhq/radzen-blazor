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
    /// A component which represents a <c>form</c>. Provides validation support.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///   &lt;RadzenTextBox style="display: block" Name="Email" @bind-Value=@model.Email /&gt;
    ///   &lt;RadzenRequiredValidator Component="Email" Text="Email is required" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///  class Model
    ///  {
    ///    public string Email { get; set; }
    ///  }
    ///
    ///  Model model = new Model();
    /// }
    /// </code>
    /// </example>
    public class RadzenTemplateForm<TItem> : RadzenComponent, IRadzenForm
    {
        /// <summary>
        /// Returns the validity of the form.
        /// </summary>
        /// <value><c>true</c> if all validators in the form a valid; otherwise, <c>false</c>.</value>
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
        /// Specifies the model of the form. Required to support validation.
        /// </summary>
        [Parameter]
        public TItem Data { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        [Parameter]
        public RenderFragment<EditContext> ChildContent { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user submits the form and <see cref="IsValid" /> is <c>true</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenTemplateForm TItem="Model" Submit=@OnSubmit Data=@model&gt;
        ///   &lt;RadzenTextBox style="display: block" Name="Email" @bind-Value=@model.Email /&gt;
        ///   &lt;RadzenRequiredValidator Component="Email" Text="Email is required" Style="position: absolute" /&gt;
        /// &lt;/RadzenTemplateForm&gt;
        /// @code {
        ///  class Model
        ///   {
        ///    public string Email { get; set; }
        ///  }
        ///
        ///  Model model = new Model();
        ///
        ///  void OnSubmit(Model value)
        ///  {
        ///
        ///  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<TItem> Submit { get; set; }

        /// <summary>
        /// Obsolete. Use <see cref="InvalidSubmit" /> instead.
        /// </summary>
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
        /// A callback that will be invoked when the user submits the form and <see cref="IsValid" /> is <c>false</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenTemplateForm TItem="Model" InvalidSubmit=@OnInvalidSubmit Data=@model&gt;
        ///   &lt;RadzenTextBox style="display: block" Name="Email" @bind-Value=@model.Email /&gt;
        ///   &lt;RadzenRequiredValidator Component="Email" Text="Email is required" Style="position: absolute" /&gt;
        /// &lt;/RadzenTemplateForm&gt;
        /// @code {
        ///  class Model
        ///  {
        ///    public string Email { get; set; }
        ///  }
        ///
        ///  Model model = new Model();
        ///
        ///  void OnInvalidSubmit(FormInvalidSubmitEventArgs args)
        ///  {
        ///
        ///  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<FormInvalidSubmitEventArgs> InvalidSubmit { get; set; }

        /// <summary>
        /// Specifies the form <c>method</c> attribute. Used together with <see cref="Action" />.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenTemplateForm TItem="Model" Method="post" Action="/register" Data=@model&gt;
        ///   &lt;RadzenTextBox style="display: block" Name="Email" @bind-Value=@model.Email /&gt;
        ///   &lt;RadzenRequiredValidator Component="Email" Text="Email is required" Style="position: absolute" /&gt;
        /// &lt;/RadzenTemplateForm&gt;
        /// </code>
        /// </example>
        [Parameter]
        public string Method { get; set; }

        /// <summary>
        /// Specifies the form <c>action</c> attribute. When set the form submits to the specified URL.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenTemplateForm TItem="Model" Method="post" Action="/register" Data=@model&gt;
        ///   &lt;RadzenTextBox style="display: block" Name="Email" @bind-Value=@model.Email /&gt;
        ///   &lt;RadzenRequiredValidator Component="Email" Text="Email is required" Style="position: absolute" /&gt;
        /// &lt;/RadzenTemplateForm&gt;
        /// </code>
        /// </example>
        [Parameter]
        public string Action { get; set; }

        private readonly Func<Task> handleSubmitDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RadzenTemplateForm{TItem}"/> class.
        /// </summary>
        public RadzenTemplateForm()
        {
            handleSubmitDelegate = OnSubmit;
        }

        /// <summary>
        /// Handles the submit event of the form.
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

        readonly List<IRadzenFormComponent> components = new List<IRadzenFormComponent>();

        /// <inheritdoc />
        public void AddComponent(IRadzenFormComponent component)
        {
            if (components.IndexOf(component) == -1)
            {
                components.Add(component);
            }
        }

        /// <inheritdoc />
        public void RemoveComponent(IRadzenFormComponent component)
        {
            components.Remove(component);
        }

        /// <inheritdoc />
        public IRadzenFormComponent FindComponent(string name)
        {
            return components.Where(component => component.Name == name).FirstOrDefault();
        }

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        [Parameter]
        public EditContext EditContext { get; set; }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (Data != null && (EditContext == null || EditContext.Model != (object)Data))
            {
                EditContext = new EditContext(Data);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-form";
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Visible)
            {
                if (EditContext != null)
                {
                    builder.OpenRegion(EditContext.GetHashCode());
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

                if (EditContext != null)
                {
                    builder.CloseRegion();
                }
            }
        }
    }
}