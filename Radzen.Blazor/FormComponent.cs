using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen
{
    public class FormComponent<T> : RadzenComponent, IRadzenFormComponent
    {
        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public int TabIndex { get; set; } = 0;

        [Parameter]
        public string Placeholder { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        IRadzenForm _form;

        [CascadingParameter]
        public EditContext EditContext { get; set; }

        [CascadingParameter]
        public IRadzenForm Form
        {
            get
            {
                return _form;
            }
            set
            {
                if (_form != value && value != null)
                {
                    _form = value;
                    _form.AddComponent(this);
                }
            }
        }

        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

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

        public bool IsBound
        {
            get
            {
                return ValueChanged.HasDelegate;
            }
        }

        protected T _value;

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


        [Parameter]
        public EventCallback<T> Change { get; set; }
        public FieldIdentifier FieldIdentifier { get; private set; }
        [Parameter]
        public Expression<Func<T>> ValueExpression { get; set; }
        public override Task SetParametersAsync(ParameterView parameters)
        {
            var result = base.SetParametersAsync(parameters);

            if (EditContext != null && ValueExpression != null && FieldIdentifier.Model != EditContext.Model)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                EditContext.OnValidationStateChanged += ValidationStateChanged;
            }

            return result;
        }

        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
            }

            Form?.RemoveComponent(this);
        }

        public object GetValue()
        {
            return Value;
        }

        public override Task OnContextMenu(MouseEventArgs args)
        {
            if (!Disabled)
            {
                return base.OnContextMenu(args);
            }

            return Task.CompletedTask;
        }
    }
}
