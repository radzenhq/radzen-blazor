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
    /// Displays and manages a collection of chips with optional selection and removal.
    /// </summary>
    /// <typeparam name="TValue">The selected value type. Use IEnumerable for multiple selection mode.</typeparam>
    public partial class RadzenChipList<TValue> : FormComponent<TValue>, IRadzenChipList
    {
        /// <summary>
        /// Gets or sets the name of the data property used as chip value.
        /// </summary>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the data property used as chip text.
        /// </summary>
        [Parameter]
        public string? TextProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the data property used for item disabled state.
        /// </summary>
        [Parameter]
        public string? DisabledProperty { get; set; }

        /// <summary>
        /// Gets or sets the data source used to generate chip items.
        /// </summary>
        [Parameter]
        public IEnumerable? Data { get; set; }

        /// <summary>
        /// Gets or sets declarative chip items.
        /// </summary>
        [Parameter]
        public RenderFragment? Items { get; set; }

        /// <summary>
        /// Gets or sets a template for custom chip content.
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenChipItem>? Template { get; set; }

        /// <summary>
        /// Gets or sets whether multiple items can be selected.
        /// </summary>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets whether chips can be removed.
        /// </summary>
        [Parameter]
        public bool AllowDelete { get; set; }

        /// <summary>
        /// Gets or sets the chip list orientation.
        /// </summary>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        /// <summary>
        /// Gets or sets the wrapping behavior.
        /// </summary>
        [Parameter]
        public FlexWrap Wrap { get; set; } = FlexWrap.Wrap;

        /// <summary>
        /// Gets or sets the default style applied to chips.
        /// </summary>
        [Parameter]
        public BadgeStyle ChipStyle { get; set; } = BadgeStyle.Base;

        /// <summary>
        /// Gets or sets the default variant applied to chips.
        /// </summary>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the default shade applied to chips.
        /// </summary>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the default chip size.
        /// </summary>
        [Parameter]
        public ChipSize Size { get; set; } = ChipSize.Medium;

        /// <summary>
        /// Gets or sets the close button accessible title.
        /// </summary>
        [Parameter]
        public string RemoveChipTitle { get; set; } = "Remove";

        /// <summary>
        /// Gets or sets the callback invoked when a chip remove action is requested.
        /// </summary>
        [Parameter]
        public EventCallback<object?> ChipRemoved { get; set; }

        readonly List<RadzenChipItem> items = new();
        List<RadzenChipItem> allItems = new();

        int focusedIndex = -1;
        bool preventKeyPress = true;

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            UpdateAllItems();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-chip-list")
                .Add(Orientation == Orientation.Horizontal ? "rz-chip-list-horizontal" : "rz-chip-list-vertical")
                .Add("rz-chip-list-nowrap", Wrap == FlexWrap.NoWrap)
                .Add("rz-chip-list-wrap-reverse", Wrap == FlexWrap.WrapReverse)
                .ToString();
        }

        void UpdateAllItems()
        {
            var dataItems = (Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
            {
                var item = new RadzenChipItem();
                item.SetText($"{PropertyAccess.GetItemOrValueFromProperty(i, TextProperty ?? string.Empty)}");
                item.SetValue(PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty ?? string.Empty));

                if (!string.IsNullOrEmpty(DisabledProperty) &&
                    PropertyAccess.TryGetItemOrValueFromProperty<bool>(i, DisabledProperty, out var disabledResult))
                {
                    item.SetDisabled(disabledResult);
                }

                return item;
            });

            allItems = items.Concat(dataItems).Where(i => i.Visible).ToList();
        }

        internal bool IsSelected(RadzenChipItem item)
        {
            if (Multiple)
            {
                return Value != null && ((IEnumerable)Value).Cast<object>().Any(v => Equals(v, item.Value));
            }

            return Equals(Value, item.Value);
        }

        internal async Task SelectItem(RadzenChipItem item)
        {
            if (Disabled || item.Disabled)
            {
                return;
            }

            focusedIndex = allItems.IndexOf(item);

            if (Multiple)
            {
                var type = typeof(TValue).IsGenericType ? typeof(TValue).GetGenericArguments()[0] : typeof(TValue);
                var selectedValues = Value != null
                    ? new List<dynamic>(((IEnumerable)Value).Cast<dynamic>())
                    : new List<dynamic>();

                if (item.Value != null && !selectedValues.Contains(item.Value))
                {
                    selectedValues.Add(item.Value);
                }
                else if (item.Value != null)
                {
                    selectedValues.Remove(item.Value);
                }

                Value = (TValue)selectedValues.AsQueryable().Cast(type);
            }
            else
            {
                Value = item.Value is TValue typedValue ? typedValue : default!;
            }

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        internal async Task RemoveItemAsync(RadzenChipItem item)
        {
            if (Disabled || item.Disabled || !AllowDelete)
            {
                return;
            }

            await ChipRemoved.InvokeAsync(item.Value);
        }

        internal EventCallback<MouseEventArgs> GetRemoveCallback(RadzenChipItem item)
        {
            return AllowDelete
                ? EventCallback.Factory.Create<MouseEventArgs>(this, args => RemoveItemAsync(item))
                : default;
        }

        internal async Task OnItemKeyDown(KeyboardEventArgs args, RadzenChipItem item)
        {
            var key = args.Code ?? args.Key;
            if (key == "Enter" || key == "Space")
            {
                await SelectItem(item);
            }
            else if ((key == "Delete" || key == "Backspace") && AllowDelete)
            {
                await RemoveItemAsync(item);
            }
        }

        internal async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            var count = allItems.Count;
            if (count == 0)
            {
                return;
            }

            if ((Orientation == Orientation.Horizontal && (key == "ArrowLeft" || key == "ArrowRight")) ||
                (Orientation == Orientation.Vertical && (key == "ArrowUp" || key == "ArrowDown")))
            {
                preventKeyPress = true;
                var direction = key == "ArrowLeft" || key == "ArrowUp" ? -1 : 1;
                focusedIndex = focusedIndex < 0 ? 0 : Math.Clamp(focusedIndex + direction, 0, count - 1);
                await InvokeAsync(StateHasChanged);
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;
                focusedIndex = key == "Home" ? 0 : count - 1;
                await InvokeAsync(StateHasChanged);
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;
                var item = focusedIndex >= 0 && focusedIndex < count ? allItems[focusedIndex] : allItems[0];
                await SelectItem(item);
            }
            else if ((key == "Delete" || key == "Backspace") && AllowDelete)
            {
                preventKeyPress = true;
                if (focusedIndex >= 0 && focusedIndex < count)
                {
                    await RemoveItemAsync(allItems[focusedIndex]);
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }

        internal string WrapStyle => Wrap switch
        {
            FlexWrap.NoWrap => "flex-wrap: nowrap;",
            FlexWrap.WrapReverse => "flex-wrap: wrap-reverse;",
            _ => "flex-wrap: wrap;"
        };

        /// <summary>
        /// Adds the specified item to the chip list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddItem(RadzenChipItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                UpdateAllItems();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the specified item from the chip list.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveItem(RadzenChipItem item)
        {
            if (items.Remove(item))
            {
                UpdateAllItems();
                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            UpdateAllItems();
            StateHasChanged();
        }
    }
}
