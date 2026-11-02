using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
        /// <summary>
        /// Gets the id of the form field label when <paramref name="owner"/> registered the label target, otherwise <c>null</c>.
        /// Components whose focusable element cannot be the target of the label <c>for</c> attribute render it as <c>aria-labelledby</c> so the label still names them.
        /// </summary>
        /// <param name="owner">The component asking for the label id.</param>
        string? GetLabelId(object owner) => null;
        /// <summary>
        /// Registers the id of the element the form field label points to with its <c>for</c> attribute.
        /// The form field uses it unless <see cref="RadzenFormField.Component"/> is set. The first component to register owns the target
        /// until it passes <c>null</c>, so components nested inside another form component do not take the label over.
        /// </summary>
        /// <param name="owner">The component registering the target.</param>
        /// <param name="id">The id of the element the label points to or <c>null</c> to release the target.</param>
        void SetLabelTarget(object owner, string? id) { }
    }

    /// <summary>
    /// Represents the context of the form field.
    /// </summary>
    public class FormFieldContext : IFormFieldContext
    {
        /// <summary>
        /// Notifies the form field that the disabled state of the component has changed.
        /// </summary>
        public Action<bool> DisabledChanged { get; set; } = null!;
        /// <summary>
        /// Gets or sets a value indicating whether the label is floating or fixed on top.
        /// </summary>
        public bool AllowFloatingLabel { get; set; }
        /// <summary>
        /// Gets or sets the id of the form field label.
        /// </summary>
        public string? LabelId { get; set; }
        /// <summary>
        /// Gets the id of the element the form field label points to, registered by the wrapped component.
        /// </summary>
        public string? LabelTarget { get; private set; }
        /// <summary>
        /// Notifies the form field that the label target has changed.
        /// </summary>
        public Action<string?>? LabelTargetChanged { get; set; }

        private object? labelTargetOwner;

        /// <inheritdoc />
        public string? GetLabelId(object owner) => labelTargetOwner == owner ? LabelId : null;

        /// <inheritdoc />
        public void SetLabelTarget(object owner, string? id)
        {
            ArgumentNullException.ThrowIfNull(owner);

            if (labelTargetOwner != null && labelTargetOwner != owner)
            {
                return;
            }

            labelTargetOwner = id != null ? owner : null;

            if (LabelTarget != id)
            {
                LabelTarget = id;
                LabelTargetChanged?.Invoke(id);
            }
        }
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
    public partial class RadzenFormField : RadzenComponent, IAsyncDisposable
    {
        private IJSObjectReference? _jsRef;
        private int _jsRefVersion;
        private bool _visibleChanged;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Visible), Visible))
            {
                _visibleChanged = true;
            }

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if ((firstRender || _visibleChanged) && JSRuntime != null)
            {
                _visibleChanged = false;

                var version = ++_jsRefVersion;
                var jsRef = _jsRef;
                _jsRef = null;

                if (jsRef != null)
                {
                    await jsRef.InvokeVoidAsync("dispose");
                    await jsRef.DisposeAsync();
                }

                if (version != _jsRefVersion)
                {
                    return;
                }

                if (Visible)
                {
                    var created = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createFormField", Element);

                    if (version == _jsRefVersion)
                    {
                        _jsRef = created;
                    }
                    else if (created != null)
                    {
                        await created.InvokeVoidAsync("dispose");
                        await created.DisposeAsync();
                    }
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _jsRefVersion++;
            var jsRef = _jsRef;
            _jsRef = null;

            if (jsRef != null)
            {
                try { await jsRef.InvokeVoidAsync("dispose"); } catch { }
                try { await jsRef.DisposeAsync(); } catch { }
            }
        }

        /// <summary>
        /// Gets or sets the input component to wrap.
        /// Place the input component (RadzenTextBox, RadzenDropDown, etc.) here.
        /// The form field automatically integrates with the input for labels and validation.
        /// </summary>
        /// <value>The input component render fragment.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets content to render before (leading position of) the input field.
        /// Typically used for icons like search, email, lock, or prefix text like currency symbols.
        /// Appears inside the form field border, before the input element.
        /// </summary>
        /// <value>The start content render fragment.</value>
        [Parameter]
        public RenderFragment? Start { get; set; }

        /// <summary>
        /// Gets or sets content to render after (trailing position of) the input field.
        /// Typically used for icons like visibility toggle, clear button, or suffix text like units.
        /// Appears inside the form field border, after the input element.
        /// </summary>
        /// <value>The end content render fragment.</value>
        [Parameter]
        public RenderFragment? End { get; set; }

        /// <summary>
        /// Gets or sets content to render below the input field.
        /// Used for helper text, hints, character counters, or validation messages.
        /// Validators placed here are automatically displayed when validation fails.
        /// </summary>
        /// <value>The helper content render fragment.</value>
        [Parameter]
        public RenderFragment? Helper { get; set; }
        /// <summary>
        /// Gets or sets the custom content for the label using a Razor template.
        /// When provided, this template will be rendered instead of the plain text specified in the Text parameter.
        /// </summary>

        [Parameter]
        public RenderFragment? TextTemplate { get; set; }
        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        [Parameter]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the label is floating or fixed on top.
        /// </summary>
        /// <value><c>true</c> if floating is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFloatingLabel { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether clicking the Start content, the End content or the empty area inside the form field border focuses the input.
        /// Clicks on interactive Start or End content such as buttons and links are not affected.
        /// </summary>
        /// <value><c>true</c> if clicking anywhere inside the form field focuses the input; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool FocusOnClick { get; set; }

        /// <summary>
        /// Gets or sets the id of the element the label is associated with, rendered as the label <c>for</c> attribute.
        /// Optional: when not set the label points to the input component placed in <see cref="ChildContent"/>.
        /// </summary>
        [Parameter]
        public string? Component { get; set; }

        /// <summary>
        /// Gets or sets the design variant of the form field.
        /// </summary>
        /// <value>The variant of the form field.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Outlined;

        private bool disabled;

        private string? labelTarget;

        private readonly FormFieldContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RadzenFormField"/> class.
        /// </summary>
        public RadzenFormField()
        {
            context = new FormFieldContext { DisabledChanged = DisabledChanged, AllowFloatingLabel = AllowFloatingLabel, LabelTargetChanged = LabelTargetChanged };
        }

        private void DisabledChanged(bool value)
        {
            disabled = value;
            StateHasChanged();
        }

        private void LabelTargetChanged(string? value)
        {
            labelTarget = value;
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            context.AllowFloatingLabel = AllowFloatingLabel;
            context.LabelId = LabelId;
        }

        string LabelId => $"{GetId()}-label";

        string? LabelFor => !string.IsNullOrEmpty(Component) ? Component : labelTarget;

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-form-field")
            .AddVariant(Variant)
            .AddDisabled(disabled)
            .Add("rz-floating-label", AllowFloatingLabel)
            .ToString();
    }
}