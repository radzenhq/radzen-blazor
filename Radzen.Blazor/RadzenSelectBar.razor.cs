using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenSelectBar<TValue> : FormComponent<TValue>, IRadzenSelectBar
    {
        ClassList ButtonClassList(RadzenSelectBarItem item) => ClassList.Create("rz-button rz-button-text-only")
                            .Add("rz-state-active", IsSelected(item))
                            .AddDisabled(Disabled);

        [Parameter]
        public string ValueProperty { get; set; }

        [Parameter]
        public string TextProperty { get; set; }

        IEnumerable<RadzenSelectBarItem> allItems
        {
            get
            {
                return items.Concat((Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
                {
                    var item = new RadzenSelectBarItem();
                    item.SetText((string)PropertyAccess.GetItemOrValueFromProperty(i, TextProperty));
                    item.SetValue(PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty));
                    return item;
                }));
            }
        }

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
                    _data = value;
                    StateHasChanged();
                }
            }
        }

        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-selectbutton rz-buttonset").Add($"rz-buttonset-{items.Count}").ToString();
        }

        [Parameter]
        public bool Multiple { get; set; }

        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenSelectBarItem> items = new List<RadzenSelectBarItem>();

        public void AddItem(RadzenSelectBarItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        public void RemoveItem(RadzenSelectBarItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        protected bool IsSelected(RadzenSelectBarItem item)
        {
            if (Value != null)
            {
                if (Multiple)
                {
                    return ((IEnumerable)Value).Cast<object>().Contains(item.Value);
                }
                else
                {
                    return object.Equals(Value, item.Value);
                }
            }

            return false;
        }

        public override bool HasValue
        {
            get
            {
                return Multiple ? Value != null && ((IEnumerable)Value).Cast<object>().Any() : Value != null;
            }
        }

        protected async System.Threading.Tasks.Task SelectItem(RadzenSelectBarItem item)
        {
            if (Disabled)
                return;

            if (Multiple)
            {
                var type = typeof(TValue).IsGenericType ? typeof(TValue).GetGenericArguments()[0] : typeof(TValue);

                var selectedValues = Value != null ? ((IEnumerable)Value).AsQueryable().Cast(type).AsEnumerable().ToList() : new List<dynamic>();

                if (!selectedValues.Contains(item.Value))
                {
                    selectedValues.Add(item.Value);
                }
                else
                {
                    selectedValues.Remove(item.Value);
                }

                Value = (TValue)selectedValues.AsQueryable().Cast(type);
            }
            else
            {
                Value = (TValue)item.Value;
            }

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }
    }
}