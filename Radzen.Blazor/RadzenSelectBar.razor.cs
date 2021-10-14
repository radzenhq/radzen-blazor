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
    /// <summary>
    /// RadzenSelectBar component.
    /// Implements the <see cref="Radzen.FormComponent{TValue}" />
    /// Implements the <see cref="Radzen.IRadzenSelectBar" />
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="Radzen.FormComponent{TValue}" />
    /// <seealso cref="Radzen.IRadzenSelectBar" />
    public partial class RadzenSelectBar<TValue> : FormComponent<TValue>, IRadzenSelectBar
    {
        ClassList ButtonClassList(RadzenSelectBarItem item) => ClassList.Create("rz-button rz-button-text-only")
                            .Add("rz-state-active", IsSelected(item))
                            .AddDisabled(Disabled);

        /// <summary>
        /// Gets or sets the value property.
        /// </summary>
        /// <value>The value property.</value>
        [Parameter]
        public string ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the text property.
        /// </summary>
        /// <value>The text property.</value>
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

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
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

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-selectbutton rz-buttonset").Add($"rz-buttonset-{items.Count}").ToString();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSelectBar{TValue}"/> is multiple.
        /// </summary>
        /// <value><c>true</c> if multiple; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenSelectBarItem> items = new List<RadzenSelectBarItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenSelectBarItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveItem(RadzenSelectBarItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        /// <summary>
        /// Determines whether the specified item is selected.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the specified item is selected; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public override bool HasValue
        {
            get
            {
                return Multiple ? Value != null && ((IEnumerable)Value).Cast<object>().Any() : Value != null;
            }
        }

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
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