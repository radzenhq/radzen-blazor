using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenButton component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenToggleButton Click=@(args => Console.WriteLine("Button clicked")) Text="ToggleButton" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenToggleButton : RadzenButton, IRadzenFormComponent
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the index of the tab.
        /// </summary>
        /// <value>The index of the tab.</value>
        [Parameter]
        public int TabIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the placeholder.
        /// </summary>
        /// <value>The placeholder.</value>
        [Parameter]
        public string Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the toggle icon.
        /// </summary>
        /// <value>The toggle icon.</value>
        [Parameter]
        public string ToggleIcon { get; set; }

        private string getIcon()
        {
            return Value && !string.IsNullOrEmpty(ToggleIcon) ? ToggleIcon : Icon;
        }

        /// <summary>
        /// The form
        /// </summary>
        IRadzenForm _form;

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        [CascadingParameter]
        public EditContext EditContext { get; set; }

        /// <summary>
        /// Gets or sets the form.
        /// </summary>
        /// <value>The form.</value>
        [CascadingParameter]
        public IRadzenForm Form
        {
            get
            {
                return _form;
            }
            set
            {
                _form = value;
                _form?.AddComponent(this);
            }
        }

        /// <summary>
        /// Gets or sets the value changed.
        /// </summary>
        /// <value>The value changed.</value>
        [Parameter]
        public EventCallback<bool> ValueChanged { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public virtual bool HasValue
        {
            get
            {
                return !object.Equals(Value, default(bool));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is bound.
        /// </summary>
        /// <value><c>true</c> if this instance is bound; otherwise, <c>false</c>.</value>
        public bool IsBound
        {
            get
            {
                return ValueChanged.HasDelegate;
            }
        }

        /// <summary>
        /// The value
        /// </summary>
        protected bool _value;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public virtual bool Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!EqualityComparer<bool>.Default.Equals(value, _value))
                {
                    _value = value;
                }
            }
        }


        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        [Parameter]
        public EventCallback<bool> Change { get; set; }
        /// <summary>
        /// Gets the field identifier.
        /// </summary>
        /// <value>The field identifier.</value>
        public FieldIdentifier FieldIdentifier { get; private set; }
        /// <summary>
        /// Gets or sets the value expression.
        /// </summary>
        /// <value>The value expression.</value>
        [Parameter]
        public Expression<Func<bool>> ValueExpression { get; set; }
        /// <summary>
        /// Sets the parameters asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task.</returns>
        public override Task SetParametersAsync(ParameterView parameters)
        {
            var disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);

            var result = base.SetParametersAsync(parameters);

            if (EditContext != null && ValueExpression != null && FieldIdentifier.Model != EditContext.Model)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
                EditContext.OnValidationStateChanged += ValidationStateChanged;
            }

            if (disabledChanged)
            {
                FormFieldContext?.DisabledChanged(Disabled);
            }

            return result;
        }

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

            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
            }

            Form?.RemoveComponent(this);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <returns>System.Object.</returns>
        public object GetValue()
        {
            return Value;
        }

        /// <summary>
        /// Handles the <see cref="E:ContextMenu" /> event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <returns>Task.</returns>
        public override Task OnContextMenu(MouseEventArgs args)
        {
            if (!Disabled)
            {
                return base.OnContextMenu(args);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the class list.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns>ClassList.</returns>
        protected ClassList GetClassList(string className) => ClassList.Create(className)
                                                                       .AddDisabled(Disabled)
                                                                       .Add(FieldIdentifier, EditContext);

        /// <summary> Provides support for RadzenFormField integration. </summary>
        [CascadingParameter]
        public IFormFieldContext FormFieldContext { get; set; }

        /// <summary> Gets the current placeholder. Returns empty string if this component is inside a RadzenFormField.</summary>
        protected string CurrentPlaceholder => FormFieldContext != null ? " " : Placeholder;
#if NET5_0_OR_GREATER
        /// <inheritdoc/>
        public virtual async ValueTask FocusAsync()
        {
            await Element.FocusAsync();
        }
#endif

        /// <summary>
        /// Toggles this instance checked state.
        /// </summary>
        async Task Toggle()
        {
            if (Disabled)
            {
                return;
            }

            Value = !Value;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        /// <inheritdoc/>
        public override async Task OnClick(MouseEventArgs args)
        {
            await base.OnClick(args);

            await Toggle();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classes = GetClassList("rz-toggle-button");

            if (HasValue)
            {
                classes.Add("rz-state-active", Value);
            }

            var baseClasses = HasValue ?
                $"rz-button rz-button-{getButtonSize()} rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()} rz-{Enum.GetName(typeof(ButtonStyle), ToggleButtonStyle).ToLowerInvariant()} rz-shade-{Enum.GetName(typeof(Shade), ToggleShade).ToLowerInvariant()}{(IsDisabled ? " rz-state-disabled" : "")}{(string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Icon) ? " rz-button-icon-only" : "")}" :
            $"rz-button rz-button-{getButtonSize()} rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()} rz-{Enum.GetName(typeof(ButtonStyle), ButtonStyle).ToLowerInvariant()} rz-shade-{Enum.GetName(typeof(Shade), Shade).ToLowerInvariant()}{(IsDisabled ? " rz-state-disabled" : "")}{(string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Icon) ? " rz-button-icon-only" : "")}";


            return $"{baseClasses} {classes.ToString()}";
        }

        /// <summary>
        /// Gets or sets the ToggleButton style.
        /// </summary>
        /// <value>The ToggleButton style.</value>
        [Parameter]
        public ButtonStyle ToggleButtonStyle { get; set; }

        /// <summary>
        /// Gets or sets the ToggleButton shade.
        /// </summary>
        /// <value>The ToggleButton shade.</value>
        [Parameter]
        public Shade ToggleShade { get; set; } = Shade.Darker;
    }
}
