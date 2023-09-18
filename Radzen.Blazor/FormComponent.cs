using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;

namespace Radzen
{
    /// <summary>
    /// Class FormComponent.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// Implements the <see cref="Radzen.IRadzenFormComponent" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Radzen.RadzenComponent" />
    /// <seealso cref="Radzen.IRadzenFormComponent" />
    public class FormComponent<T> : RadzenComponent, IRadzenFormComponent
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
        /// Gets or sets a value indicating whether this <see cref="FormComponent{T}"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

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
        public EventCallback<T> ValueChanged { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public virtual bool HasValue
        {
            get
            {
                if (typeof(T) == typeof(string))
                {
                    return !string.IsNullOrEmpty($"{Value}");
                }
                return !object.Equals(Value, default(T));
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
        protected T _value;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public virtual T Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(value, _value))
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
        public EventCallback<T> Change { get; set; }
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
        public Expression<Func<T>> ValueExpression { get; set; }
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
                                                                       .Add(FieldIdentifier, EditContext)
                                                                       .Add("rz-state-empty", !HasValue);

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
    }
}
