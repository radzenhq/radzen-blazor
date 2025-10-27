using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface that represents the context of the form field.
    /// </summary>
    public interface IFormFieldContext
    {
        /// <summary>
        /// Notifies the form field that the disabled state of the component has changed.
        /// </summary>
        Action<bool> DisabledChanged { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the label is floating or fixed on top.
        /// </summary>
        bool AllowFloatingLabel { get; set; }
    }

    /// <summary>
    /// Represents the context of the form field.
    /// </summary>
    public class FormFieldContext : IFormFieldContext
    {
        /// <summary>
        /// Notifies the form field that the disabled state of the component has changed.
        /// </summary>
        public Action<bool> DisabledChanged { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the label is floating or fixed on top.
        /// </summary>
        public bool AllowFloatingLabel { get; set; }
    }

    /// <summary>
    /// A form field container component that wraps input components with labels, icons, helper text, and validation messages.
    /// RadzenFormField provides a Material Design-style field layout with floating labels and consistent spacing.
    /// Enhances form inputs by adding structure, labels, and supplementary content in a cohesive layout.
    /// Features top-aligned or floating labels via Text property, Start/End content for icons or buttons before/after the input (e.g., search icon, clear button),
    /// helper text for explanatory text or validation messages below the input, Filled/Outlined/Flat variants matching Material Design,
    /// floating labels that animate upward when input is focused or has value, and automatic display of validation messages when used with validators.
    /// Compatible with RadzenTextBox, RadzenTextArea, RadzenPassword, RadzenDropDown, RadzenNumeric, RadzenDatePicker, and similar input components.
    /// Use Start for leading icons (search, email), End for trailing icons (visibility toggle, clear button).
    /// </summary>
    /// <example>
    /// Basic form field with label:
    /// <code>
    /// &lt;RadzenFormField Text="Email Address"&gt;
    ///     &lt;RadzenTextBox @bind-Value=@email /&gt;
    /// &lt;/RadzenFormField&gt;
    /// </code>
    /// Form field with icon and validation:
    /// <code>
    /// &lt;RadzenTemplateForm Data=@model&gt;
    ///     &lt;RadzenFormField Text="Search" Variant="Variant.Outlined"&gt;
    ///         &lt;Start&gt;&lt;RadzenIcon Icon="search" /&gt;&lt;/Start&gt;
    ///         &lt;ChildContent&gt;
    ///             &lt;RadzenTextBox Name="SearchTerm" @bind-Value=@model.SearchTerm /&gt;
    ///         &lt;/ChildContent&gt;
    ///         &lt;Helper&gt;
    ///             &lt;RadzenRequiredValidator Component="SearchTerm" Text="Search term is required" /&gt;
    ///         &lt;/Helper&gt;
    ///     &lt;/RadzenFormField&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// </code>
    /// Floating label form field:
    /// <code>
    /// &lt;RadzenFormField Text="Username" AllowFloatingLabel="true" Variant="Variant.Filled"&gt;
    ///     &lt;RadzenTextBox /&gt;
    /// &lt;/RadzenFormField&gt;
    /// </code>
    /// </example>
    public partial class RadzenFormField : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the input component to wrap.
        /// Place the input component (RadzenTextBox, RadzenDropDown, etc.) here.
        /// The form field automatically integrates with the input for labels and validation.
        /// </summary>
        /// <value>The input component render fragment.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets content to render before (leading position of) the input field.
        /// Typically used for icons like search, email, lock, or prefix text like currency symbols.
        /// Appears inside the form field border, before the input element.
        /// </summary>
        /// <value>The start content render fragment.</value>
        [Parameter]
        public RenderFragment Start { get; set; }

        /// <summary>
        /// Gets or sets content to render after (trailing position of) the input field.
        /// Typically used for icons like visibility toggle, clear button, or suffix text like units.
        /// Appears inside the form field border, after the input element.
        /// </summary>
        /// <value>The end content render fragment.</value>
        [Parameter]
        public RenderFragment End { get; set; }

        /// <summary>
        /// Gets or sets content to render below the input field.
        /// Used for helper text, hints, character counters, or validation messages.
        /// Validators placed here are automatically displayed when validation fails.
        /// </summary>
        /// <value>The helper content render fragment.</value>
        [Parameter]
        public RenderFragment Helper { get; set; }
        /// <summary>
        /// Gets or sets the custom content for the label using a Razor template.
        /// When provided, this template will be rendered instead of the plain text specified in the Text parameter.
        /// </summary>

        [Parameter]
        public RenderFragment TextTemplate { get; set; }
        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the label is floating or fixed on top.
        /// </summary>
        /// <value><c>true</c> if floating is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFloatingLabel { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the form field. Used to associate the label with a component.
        /// </summary>
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


        /// <summary>
        /// Initializes a new instance of the <see cref="RadzenFormField"/> class.
        /// </summary>
        public RadzenFormField()
        {
            context = new FormFieldContext { DisabledChanged = DisabledChanged, AllowFloatingLabel = AllowFloatingLabel };
        }

        private void DisabledChanged(bool value)
        {
            disabled = value;
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            context.AllowFloatingLabel = AllowFloatingLabel;
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-form-field")
            .AddVariant(Variant)
            .AddDisabled(disabled)
            .Add("rz-floating-label", AllowFloatingLabel)
            .ToString();
    }
}