using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenCheckBoxList<TValue> : FormComponent<IEnumerable<TValue>>
    {
        ClassList ItemClassList(RadzenCheckBoxListItem<TValue> item) => ClassList.Create("rz-chkbox-box")
                                                                            .Add("rz-state-active", IsSelected(item))
                                                                            .AddDisabled(Disabled || item.Disabled);

        ClassList IconClassList(RadzenCheckBoxListItem<TValue> item) => ClassList.Create("rz-chkbox-icon")
                                                                            .Add("rzi rzi-check", IsSelected(item));

        [Parameter]
        public string ValueProperty { get; set; }

        [Parameter]
        public string TextProperty { get; set; }

        IEnumerable<RadzenCheckBoxListItem<TValue>> allItems
        {
            get
            {
                return items.Concat((Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
                {
                    var item = new RadzenCheckBoxListItem<TValue>();
                    item.SetText((string)PropertyAccess.GetItemOrValueFromProperty(i, TextProperty));
                    item.SetValue((TValue)PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty));
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
            return GetClassList(Orientation == Orientation.Horizontal ? "rz-checkbox-list-horizontal" : "rz-checkbox-list-vertical").ToString();
        }

        public override bool HasValue
        {
            get
            {
                return Value != null && Value.Any();
            }
        }

        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenCheckBoxListItem<TValue>> items = new List<RadzenCheckBoxListItem<TValue>>();

        public void AddItem(RadzenCheckBoxListItem<TValue> item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        public void RemoveItem(RadzenCheckBoxListItem<TValue> item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        protected bool IsSelected(RadzenCheckBoxListItem<TValue> item)
        {
            return Value != null && Value.Contains(item.Value);
        }

        protected async System.Threading.Tasks.Task SelectItem(RadzenCheckBoxListItem<TValue> item)
        {
            if (Disabled || item.Disabled)
                return;

            List<TValue> selectedValues = new List<TValue>(Value != null ? Value : Enumerable.Empty<TValue>());

            if (!selectedValues.Contains(item.Value))
            {
                selectedValues.Add(item.Value);
            }
            else
            {
                selectedValues.Remove(item.Value);
            }

            Value = selectedValues;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        private string getDisabledState(RadzenCheckBoxListItem<TValue> item)
        {
            return Disabled || item.Disabled ? " rz-state-disabled" : "";
        }
    }
}