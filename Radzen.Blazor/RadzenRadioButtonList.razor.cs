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
    /// RadzenRadioButtonList component.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenRadioButtonList @bind-Value=@value TValue="int" Orientation="Orientation.Vertical" &gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenRadioButtonListItem Text="Orders" Value="1" /&gt;
    ///         &lt;RadzenRadioButtonListItem Text="Employees" Value="2" /&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenRadioButtonList&gt;
    /// </code>
    /// </example>
    public partial class RadzenRadioButtonList<TValue> : FormComponent<TValue>
    {
        ClassList ItemClassList(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-box")
                                                                            .Add("rz-state-active", IsSelected(item))
                                                                            .Add("rz-state-focused", IsFocused(item))
                                                                            .AddDisabled(Disabled || item.Disabled);

        ClassList IconClassList(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-icon")
                                                                            .Add("notranslate rzi rzi-circle-on", IsSelected(item));
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

        /// <summary>
        /// Gets or sets the disabled property.
        /// </summary>
        /// <value>The disabled property.</value>
        [Parameter]
        public string DisabledProperty { get; set; }

        /// <summary>
        /// Gets or sets the visible property.
        /// </summary>
        /// <value>The visible property.</value>
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
        protected override string GetComponentCssClass()
        {
            return GetClassList(Orientation == Orientation.Horizontal ? "rz-radio-button-list-horizontal" : "rz-radio-button-list-vertical").ToString();
        }

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        /// <value>The orientation.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenRadioButtonListItem<TValue>> items = new List<RadzenRadioButtonListItem<TValue>>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenRadioButtonListItem<TValue> item)
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

        /// <summary>
        /// Determines whether the specified item is selected.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the specified item is selected; otherwise, <c>false</c>.</returns>
        protected bool IsSelected(RadzenRadioButtonListItem<TValue> item)
        {
            return object.Equals(Value, item.Value);
        }

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected async System.Threading.Tasks.Task SelectItem(RadzenRadioButtonListItem<TValue> item)
        {
            if (Disabled || item.Disabled)
                return;

            focusedIndex = -1;

            Value = item.Value;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null)
            { EditContext?.NotifyFieldChanged(FieldIdentifier); }
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

        internal int focusedIndex = -1;
        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            var item = items.ElementAtOrDefault(focusedIndex) ?? items.FirstOrDefault();

            if (item == null) return;

            if (key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyPress = true;

                focusedIndex = items.IndexOf(items.FirstOrDefault(i => IsSelected(i)) ?? item);

                focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowLeft" ? -1 : 1), 0, items.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count() - 1);
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;

                focusedIndex = key == "Home" ? 0 : items.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count() - 1;
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < items.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count())
                {
                    await SelectItem(items.Where(t => HasInvisibleBefore(item) ? true : t.Visible).ToList()[focusedIndex]);
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }

        bool HasInvisibleBefore(RadzenRadioButtonListItem<TValue> item)
        {
            return items.Take(items.IndexOf(item)).Any(t => !t.Visible && !t.Disabled);
        }

        bool IsFocused(RadzenRadioButtonListItem<TValue> item)
        {
            return items.IndexOf(item) == focusedIndex;
        }
    }
}
