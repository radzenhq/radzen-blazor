using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A radio button group component that allows users to select a single option from a list of choices.
    /// RadzenRadioButtonList displays multiple radio buttons with configurable layout, orientation, and data binding.
    /// Presents mutually exclusive options where only one can be selected at a time.
    /// Supports data binding via Data property or static item declaration, configurable layout including orientation (Horizontal/Vertical), gap spacing, wrapping, alignment, and justification,
    /// custom item templates for complex radio button content, disabled items individually or for the entire list, and keyboard navigation (Arrow keys, Space, Enter) for accessibility.
    /// Use for forms where users must choose one option from several, like payment methods, shipping options, or preference settings.
    /// </summary>
    /// <typeparam name="TValue">The type of the selected value. Each radio button option has a value of this type.</typeparam>
    /// <example>
    /// Static radio button list:
    /// <code>
    /// &lt;RadzenRadioButtonList @bind-Value=@selectedOption TValue="string" Orientation="Orientation.Vertical"&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenRadioButtonListItem Text="Option 1" Value="option1" /&gt;
    ///         &lt;RadzenRadioButtonListItem Text="Option 2" Value="option2" /&gt;
    ///         &lt;RadzenRadioButtonListItem Text="Option 3" Value="option3" /&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenRadioButtonList&gt;
    /// </code>
    /// Data-bound radio list with disabled item:
    /// <code>
    /// &lt;RadzenRadioButtonList @bind-Value=@paymentMethod TValue="int" Data=@paymentMethods 
    ///                         TextProperty="Name" ValueProperty="Id" DisabledProperty="IsDisabled" /&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    [CascadingTypeParameter(nameof(TValue))]
    public partial class RadzenRadioButtonList<TValue> : FormComponent<TValue>, IRadzenRadioButtonList
    {
        string ItemClass(IRadzenRadioButtonListItem item) => ClassList.Create("rz-radiobutton-box")
                                                                            .Add("rz-state-active", IsSelected(item))
                                                                            .Add("rz-state-focused", IsFocused(item) && focused)
                                                                            .AddDisabled(Disabled || item.Disabled)
                                                                            .ToString();

        string IconClass(IRadzenRadioButtonListItem item) => ClassList.Create("rz-radiobutton-icon")
                                                                             .Add("notranslate rzi rzi-circle-on", IsSelected(item))
                                                                             .ToString();

        string LabelClass(IRadzenRadioButtonListItem item) => ClassList.Create("rz-radiobutton-label")
                                                                             .AddDisabled(Disabled || item.Disabled)
                                                                             .ToString();

        /// <summary>
        /// Gets or sets the value property.
        /// </summary>
        /// <value>The value property.</value>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the text property.
        /// </summary>
        /// <value>The text property.</value>
        [Parameter]
        public string? TextProperty { get; set; }

        /// <summary>
        /// Gets or sets the content justify.
        /// </summary>
        /// <value>The content justify.</value>
        [Parameter]
        public JustifyContent JustifyContent { get; set; } = JustifyContent.Start;

        /// <summary>
        /// Gets or sets the items alignment.
        /// </summary>
        /// <value>The items alignment.</value>
        [Parameter]
        public AlignItems AlignItems { get; set; } = AlignItems.Start;

        /// <summary>
        /// Gets or sets the spacing between items
        /// </summary>
        /// <value>The spacing between items.</value>
        [Parameter]
        public string? Gap { get; set; }

        /// <summary>
        /// Gets or sets the wrap.
        /// </summary>
        /// <value>The wrap.</value>
        [Parameter]
        public FlexWrap Wrap { get; set; } = FlexWrap.Wrap;

        /// <summary>
        /// Gets or sets the disabled property.
        /// </summary>
        /// <value>The disabled property.</value>
        [Parameter]
        public string? DisabledProperty { get; set; }

        /// <summary>
        /// Gets or sets the visible property.
        /// </summary>
        /// <value>The visible property.</value>
        [Parameter]
        public string? VisibleProperty { get; set; }

        List<IRadzenRadioButtonListItem> allItems = new();

        void UpdateAllItems()
        {
            allItems = items.Concat((Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
            {
                var item = new RadzenRadioButtonListItem<TValue>();
                item.SetText((string?)PropertyAccess.GetItemOrValueFromProperty(i, TextProperty ?? string.Empty) ?? string.Empty);
                item.SetValue((TValue)PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty ?? string.Empty)!);

                if (DisabledProperty != null && PropertyAccess.TryGetItemOrValueFromProperty<bool>(i, DisabledProperty, out var disabledResult))
                {
                    item.SetDisabled(disabledResult);
                }

                if (VisibleProperty != null && PropertyAccess.TryGetItemOrValueFromProperty<bool>(i, VisibleProperty, out var visibleResult))
                {
                    item.SetVisible(visibleResult);
                }

                return (IRadzenRadioButtonListItem)item;
            })).ToList();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            UpdateAllItems();
        }

        private IEnumerable? data;

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public virtual IEnumerable? Data
        {
            get
            {
                return data;
            }
            set
            {
                if (data != value)
                {
                    data = value;
                    StateHasChanged();
                }
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var horizontal = Orientation == Orientation.Horizontal;

            return $"rz-radio-button-list rz-radio-button-list-{(horizontal ? "horizontal" : "vertical")}";
            
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
        public RenderFragment? Items { get; set; }

        List<IRadzenRadioButtonListItem> items = new List<IRadzenRadioButtonListItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenRadioButtonListItem<TValue> item)
        {
            AddItem((IRadzenRadioButtonListItem)item);
        }

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(IRadzenRadioButtonListItem item)
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
        public void RemoveItem(RadzenRadioButtonListItem<TValue> item)
        {
            RemoveItem((IRadzenRadioButtonListItem)item);
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveItem(IRadzenRadioButtonListItem item)
        {
            if (items.Remove(item))
            {
                UpdateAllItems();
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
        protected bool IsSelected(IRadzenRadioButtonListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return object.Equals(Value, item.Value);
        }

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected async System.Threading.Tasks.Task SelectItem(IRadzenRadioButtonListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (Disabled || item.Disabled)
            {
                return;
            }

            focusedIndex = allItems.IndexOf(item);

            Value = item.Value is TValue typedValue ? typedValue : default!;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null)
            { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        async Task OnItemKeyDown(KeyboardEventArgs args, IRadzenRadioButtonListItem item)
        {
            var key = args.Code != null ? args.Code : args.Key;
            if (key == "Enter" || key == "Space")
            {
                await SelectItem(item);
            }
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
        bool stopKeydownPropagation;

        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            var item = allItems.ElementAtOrDefault(focusedIndex) ?? allItems.FirstOrDefault();

            if (item == null)
            {
                return;
            }

            if (key == "ArrowLeft" || key == "ArrowRight" || key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                var direction = key == "ArrowLeft" || key == "ArrowUp" ? -1 : 1;
                var next = FindNextSelectable(focusedIndex, direction);

                if (next >= 0 && next != focusedIndex)
                {
                    await SelectItem(allItems[next]);
                }
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                var next = key == "Home" ? FindNextSelectable(-1, 1) : FindNextSelectable(allItems.Count, -1);

                if (next >= 0)
                {
                    await SelectItem(allItems[next]);
                }
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                if (!item.Disabled && item.Visible)
                {
                    await SelectItem(item);
                }
            }
            else
            {
                preventKeyPress = false;
                stopKeydownPropagation = false;
            }
        }

        int FindNextSelectable(int from, int direction)
        {
            var count = allItems.Count;

            if (count == 0)
            {
                return -1;
            }

            for (var step = 1; step <= count; step++)
            {
                var index = from + direction * step;
                index = ((index % count) + count) % count;

                var candidate = allItems[index];

                if (candidate.Visible && !candidate.Disabled)
                {
                    return index;
                }
            }

            return -1;
        }

        string? ActiveDescendantId => focusedIndex >= 0 && focusedIndex < allItems.Count && focused
            ? allItems[focusedIndex].GetItemId()
            : null;

        /// <summary>
        /// Gets or sets the id of an external element that labels the radio group. Sets the aria-labelledby attribute.
        /// </summary>
        /// <value>The aria-labelledby value.</value>
        [Parameter]
        public string? AriaLabelledBy { get; set; }

        string? GroupAriaLabel => (Attributes != null && Attributes.ContainsKey("aria-label")) || !string.IsNullOrEmpty(AriaLabelledBy)
            ? null
            : Name;

        bool IsFocused(IRadzenRadioButtonListItem item)
        {
            return allItems.IndexOf(item) == focusedIndex;
        }
        void OnFocus()
        {
            if (focusedIndex < 0 || focusedIndex >= allItems.Count)
            {
                var selected = allItems.FindIndex(IsSelected);
                focusedIndex = selected >= 0 ? selected : FindNextSelectable(-1, 1);
            }

            focused = true;
        }
        void OnBlur()
        {
            focused = false;
        }
    }
}
