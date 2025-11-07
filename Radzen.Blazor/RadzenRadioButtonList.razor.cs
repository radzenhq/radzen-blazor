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
    public partial class RadzenRadioButtonList<TValue> : FormComponent<TValue>
    {
        string ItemClass(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-box")
                                                                            .Add("rz-state-active", IsSelected(item))
                                                                            .Add("rz-state-focused", IsFocused(item) && focused)
                                                                            .AddDisabled(Disabled || item.Disabled)
                                                                            .ToString();

        string IconClass(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-icon")
                                                                             .Add("notranslate rzi rzi-circle-on", IsSelected(item))
                                                                             .ToString();

        string LabelClass(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-label")
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
        public string Gap { get; set; }

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
        public string DisabledProperty { get; set; }

        /// <summary>
        /// Gets or sets the visible property.
        /// </summary>
        /// <value>The visible property.</value>
        [Parameter]
        public string VisibleProperty { get; set; }

        List<RadzenRadioButtonListItem<TValue>> allItems;

        void UpdateAllItems()
        {
            allItems = items.Concat((Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
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
            })).ToList();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            UpdateAllItems();
        }

        private IEnumerable data = null;

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public virtual IEnumerable Data
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
            if (items.Contains(item))
            {
                items.Remove(item);
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

            focusedIndex = allItems.IndexOf(item);

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

        bool focused;
        int focusedIndex = -1;
        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            var item = allItems.ElementAtOrDefault(focusedIndex) ?? allItems.FirstOrDefault();

            if (item == null) return;

            if ((Orientation == Orientation.Horizontal && (key == "ArrowLeft" || key == "ArrowRight")) ||
                (Orientation == Orientation.Vertical && (key == "ArrowUp" || key == "ArrowDown")))
            {
                preventKeyPress = true;
                var direction = key == "ArrowLeft" || key == "ArrowUp" ? -1 : 1;

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

        bool HasInvisibleBefore(RadzenRadioButtonListItem<TValue> item)
        {
            return allItems.Take(allItems.IndexOf(item)).Any(t => !t.Visible && !t.Disabled);
        }

        bool IsFocused(RadzenRadioButtonListItem<TValue> item)
        {
            return allItems.IndexOf(item) == focusedIndex;
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
