using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    public partial class RadzenRadioButtonList<TValue> : FormComponent<TValue>
    {
        ClassList ItemClassList(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-box")
                                                                            .Add("rz-state-active", IsSelected(item))
                                                                            .AddDisabled(Disabled || item.Disabled);

        ClassList IconClassList(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-icon")
                                                                            .Add("rzi rzi-circle-on", IsSelected(item));
        [Parameter]
        public string ValueProperty { get; set; }

        [Parameter]
        public string TextProperty { get; set; }

        [Parameter]
        public string DisabledProperty { get; set; }

        [Parameter]
        public string VisibleProperty { get; set; }

        IEnumerable<RadzenRadioButtonListItem<TValue>> allItems
        {
            get
            {
                return items.Concat((Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
                {
                    var item = new RadzenRadioButtonListItem<TValue>();
                    item.SetText((string)PropertyAccess.GetItemOrValueFromProperty(i, TextProperty));
                    item.SetValue((TValue)PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty));

                    if (DisabledProperty != null && PropertyAccess.TryGetItemOrValueFromProperty<bool>(i, DisabledProperty, out var disabledResult))
                    {
                        item.SetDisabled(disabledResult);
                    }

                    if (VisibleProperty != null && PropertyAccess.TryGetItemOrValueFromProperty<bool>(i, VisibleProperty, out var visibleResult))
                    {
                        item.SetVisible(visibleResult);
                    }

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
            return GetClassList(Orientation == Orientation.Horizontal ? "rz-radio-button-list-horizontal" : "rz-radio-button-list-vertical").ToString();
        }

        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenRadioButtonListItem<TValue>> items = new List<RadzenRadioButtonListItem<TValue>>();

        public void AddItem(RadzenRadioButtonListItem<TValue> item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        public void RemoveItem(RadzenRadioButtonListItem<TValue> item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                try
                { InvokeAsync(StateHasChanged); }
                catch { }
            }
        }

        protected bool IsSelected(RadzenRadioButtonListItem<TValue> item)
        {
            return object.Equals(Value, item.Value);
        }

        protected async System.Threading.Tasks.Task SelectItem(RadzenRadioButtonListItem<TValue> item)
        {
            if (Disabled || item.Disabled)
                return;

            Value = item.Value;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null)
            { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        public void Refresh()
        {
            StateHasChanged();
        }
    }
}