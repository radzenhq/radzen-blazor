using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSelectBar component.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenSelectBar @bind-Value=@values TValue="IEnumerable&lt;int&gt;" Multiple="true"&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenSelectBarItem Text="Orders" Value="1" /&gt;
    ///         &lt;RadzenSelectBarItem Text="Employees" Value="2" /&gt;
    ///         &lt;RadzenSelectBarItem Text="Customers" Value="3" /&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenSelectBar&gt;
    /// </code>
    /// </example>
    public partial class RadzenSelectBar<TValue> : FormComponent<TValue>, IRadzenSelectBar
    {
        /// <summary>
        /// Gets or sets the size. Set to <c>ButtonSize.Medium</c> by default.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets the orientation. Set to <c>Orientation.Horizontal</c> by default.
        /// </summary>
        /// <value>The orientation.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;


        string ButtonClass(RadzenSelectBarItem item) => ClassList.Create($"rz-button rz-button-text-only")
                                                                 .AddButtonSize(Size)
                                                                 .Add("rz-state-active", IsSelected(item))
                                                                 .Add("rz-state-focused", IsFocused(item) && focused)
                                                                 .AddDisabled(Disabled || item.Disabled)
                                                                 .ToString();

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

        List<RadzenSelectBarItem> allItems;

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

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            UpdateAllItems();
        }

        void UpdateAllItems()
        {
            allItems = items.Concat((Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
            {
                var item = new RadzenSelectBarItem();
                item.SetText($"{PropertyAccess.GetItemOrValueFromProperty(i, TextProperty)}");
                item.SetValue(PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty));
                return item;
            })).ToList();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => GetClassList("rz-selectbar rz-buttonset")
                                                            .Add($"rz-selectbar-{(Orientation == Orientation.Vertical ? "vertical" : "horizontal")}")
                                                            .Add($"rz-buttonset-{allItems.Count}")
                                                            .ToString();

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
                UpdateAllItems();
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
                UpdateAllItems();
                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified item is selected.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the specified item is selected; otherwise, <c>false</c>.</returns>
        protected bool IsSelected(RadzenSelectBarItem item)
        {
            if (Multiple)
            {
                return Value != null && ((IEnumerable)Value).Cast<object>().Contains(item.Value);
            }
            else
            {
                return object.Equals(Value, item.Value);
            }
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
        protected async Task SelectItem(RadzenSelectBarItem item)
        {
            if (Disabled || item.Disabled)
                return;

            focusedIndex = allItems.IndexOf(item);

            if (Multiple)
            {
                var type = typeof(TValue).IsGenericType ? typeof(TValue).GetGenericArguments()[0] : typeof(TValue);

                var selectedValues = Value != null ? new List<dynamic>(((IEnumerable)Value).Cast<dynamic>()) : new List<dynamic>();

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

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            StateHasChanged();
        }

        bool focused;
        int focusedIndex = -1;
        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            var item = allItems.ElementAtOrDefault(focusedIndex) ?? allItems.FirstOrDefault();

            if (item == null) return;

            if (key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyPress = true;

                var direction = key == "ArrowLeft" ? -1 : 1;

                focusedIndex = Math.Clamp(focusedIndex + direction, 0, allItems.FindLastIndex(t => t.Visible && !t.Disabled));

                while (allItems.ElementAtOrDefault(focusedIndex)?.Disabled == true)
                {
                    focusedIndex = focusedIndex + direction;
                }               
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;

                focusedIndex = key == "Home" ? 0 : allItems.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count() - 1;
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < allItems.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count())
                {
                    await SelectItem(allItems.Where(t => HasInvisibleBefore(item) ? true : t.Visible).ToList()[focusedIndex]);
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }

        bool HasInvisibleBefore(RadzenSelectBarItem item)
        {
            return allItems.Take(allItems.IndexOf(item)).Any(t => !t.Visible && !t.Disabled);
        }

        bool IsFocused(RadzenSelectBarItem item)
        {
            return allItems.ToList().IndexOf(item) == focusedIndex;
        }

        void OnFocus()
        {
            focusedIndex = focusedIndex == -1 ? 0 : focusedIndex;
            focused = true;
        }
        void OnBlur()
        {
            focused = false;
        }
    }
}