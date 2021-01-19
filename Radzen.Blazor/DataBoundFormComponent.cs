using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen.Blazor;

namespace Radzen
{
    public class DataBoundFormComponent<T> : RadzenComponent, IRadzenFormComponent
    {
        [Parameter]
        public int TabIndex { get; set; } = 0;

        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;

        [Parameter]
        public StringFilterOperator FilterOperator { get; set; } = StringFilterOperator.Contains;

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public string Placeholder { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public EventCallback<object> Change { get; set; }

        [Parameter]
        public EventCallback<Radzen.LoadDataArgs> LoadData { get; set; }

        IRadzenForm _form;

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

        object _value;
        [Parameter]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value != value)
                {
                    _value = object.Equals(value, "null") ? null : value;
                }
            }
        }

        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

        public bool IsBound
        {
            get
            {
                return ValueChanged.HasDelegate;
            }
        }
        public bool HasValue
        {
            get
            {
                if (typeof(T) == typeof(string))
                {
                    return !string.IsNullOrEmpty($"{Value}");
                }
                return Value != null;
            }
        }

        [Parameter]
        public string TextProperty { get; set; }

        IEnumerable _data = null;
        [Parameter]
        public virtual IEnumerable Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (_data != value)
                {
                    _view = null;
                    _value = null;
                    _data = value;
                    OnDataChanged();
                    StateHasChanged();
                }
            }
        }

        protected virtual void OnDataChanged()
        {

        }

        protected virtual IQueryable Query
        {
            get
            {
                return Data != null ? Data.AsQueryable() : null;
            }
        }

        protected string searchText;

        protected IQueryable _view = null;
        protected virtual IEnumerable View
        {
            get
            {
                if (_view == null && Query != null)
                {
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        var ignoreCase = FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive;

                        var query = new List<string>();

                        if (!string.IsNullOrEmpty(TextProperty))
                        {
                            query.Add(TextProperty);
                        }

                        if (ignoreCase)
                        {
                            query.Add("ToLower()");
                        }

                        query.Add($"{Enum.GetName(typeof(StringFilterOperator), FilterOperator)}(@0)");

                        _view = Query.Where(String.Join(".", query), ignoreCase ? searchText.ToLower() : searchText);
                    }
                    else
                    {
                        _view = (typeof(IQueryable).IsAssignableFrom(Data.GetType())) ? Query.Cast<object>().ToList().AsQueryable() : Query;
                    }
                }

                return _view;
            }
        }

        [CascadingParameter]
        public EditContext EditContext { get; set; }

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
    }
}
