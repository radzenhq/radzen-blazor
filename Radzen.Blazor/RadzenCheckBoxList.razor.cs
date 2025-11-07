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
    /// A checkbox group component that allows users to select multiple options from a list of choices.
    /// RadzenCheckBoxList displays multiple checkboxes with configurable layout, orientation, and data binding, binding to a collection of selected values.
    /// Allows multiple selections, unlike radio button lists. The bound value is a collection of all checked items.
    /// Supports multiple selection where users can check/uncheck any number of items, data binding via Data property or static item declaration,
    /// configurable layout including orientation (Horizontal/Vertical), gap spacing, wrapping, alignment, and justification,
    /// custom item templates for complex checkbox content, disabled/read-only items individually or for the entire list, and keyboard navigation (Arrow keys, Space, Enter) for accessibility.
    /// The Value property is IEnumerable&lt;TValue&gt; containing all selected item values. Common uses include multi-select filters, preference selections, or feature toggles.
    /// </summary>
    /// <typeparam name="TValue">The type of individual item values. The bound Value is IEnumerable&lt;TValue&gt; containing all selected items.</typeparam>
    /// <example>
    /// Static checkbox list:
    /// <code>
    /// &lt;RadzenCheckBoxList @bind-Value=@selectedOptions TValue="string"&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenCheckBoxListItem Text="Email notifications" Value="email" /&gt;
    ///         &lt;RadzenCheckBoxListItem Text="SMS notifications" Value="sms" /&gt;
    ///         &lt;RadzenCheckBoxListItem Text="Push notifications" Value="push" /&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenCheckBoxList&gt;
    /// @code {
    ///     IEnumerable&lt;string&gt; selectedOptions = new[] { "email" };
    /// }
    /// </code>
    /// Data-bound checkbox list:
    /// <code>
    /// &lt;RadzenCheckBoxList @bind-Value=@selectedIds TValue="int" Data=@categories 
    ///                      TextProperty="Name" ValueProperty="Id" Orientation="Orientation.Horizontal" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenCheckBoxList<TValue> : FormComponent<IEnumerable<TValue>>
    {
        string ItemClass(RadzenCheckBoxListItem<TValue> item) => ClassList.Create("rz-chkbox-box")
                                                                          .Add("rz-state-active", IsSelected(item))
                                                                          .Add("rz-state-focused", IsFocused(item) && focused)
                                                                          .AddDisabled(Disabled || item.Disabled)
                                                                          .ToString();

        string IconClass(RadzenCheckBoxListItem<TValue> item) => ClassList.Create("rz-chkbox-icon")
                                                                          .Add("notranslate rzi rzi-check", IsSelected(item))
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
        /// Gets or sets the read-only property.
        /// </summary>
        /// <value>The read-only property.</value>
        [Parameter]
        public string ReadOnlyProperty { get; set; }

        void UpdateAllItems()
        {
            allItems = items.Concat((Data != null ? Data.Cast<object>() : Enumerable.Empty<object>()).Select(i =>
            {
                var item = new RadzenCheckBoxListItem<TValue>();
                item.SetText((string)PropertyAccess.GetItemOrValueFromProperty(i, TextProperty));
                item.SetValue((TValue)PropertyAccess.GetItemOrValueFromProperty(i, ValueProperty));

                if (DisabledProperty != null && PropertyAccess.TryGetItemOrValueFromProperty<bool>(i, DisabledProperty, out var disabledResult))
                {
                    item.SetDisabled(disabledResult);
                }

                if (ReadOnlyProperty != null && PropertyAccess.TryGetItemOrValueFromProperty<bool>(i, ReadOnlyProperty, out var readOnlyResult))
                {
                    item.SetReadOnly(readOnlyResult);
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

        List<RadzenCheckBoxListItem<TValue>> allItems;
        /// <summary>
        /// Gets or sets a value indicating whether the user can select all values. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if select all values is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSelectAll { get; set; } = false;

        /// <summary>
        /// Gets or sets the select all text.
        /// </summary>
        /// <value>The select all text.</value>
        [Parameter]
        public string SelectAllText { get; set; }

        async Task SelectAll(bool? value)
        {
            if (Disabled || ReadOnly)
            {
                return;
            }

            if (value == true)
            {
                Value = allItems.Where(i => !i.Disabled).Select(i => i.Value);
            }
            else if (value == false)
            {
                Value = null;
            }

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        bool? IsAllSelected()
        {
            Func<RadzenCheckBoxListItem<TValue>, bool> predicate = i => Value != null && Value.Contains(i.Value);
            var all = allItems.All(predicate);
            var any = allItems.Any(predicate);

            if (all)
            {
                return true;
            }
            else
            {
                return any ? null : (bool?)false;
            }
        }

        private IEnumerable data = null;

        /// <summary>
        /// Gets or sets the data used to generate items.
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

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-checkbox-list").Add(Orientation == Orientation.Horizontal ? "rz-checkbox-list-horizontal" : "rz-checkbox-list-vertical").ToString();
        }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public override bool HasValue
        {
            get
            {
                return Value != null && Value.Any();
            }
        }

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        /// <value>The orientation.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        /// <summary>
        /// Gets or sets the items that will be concatenated with generated items from Data.
        /// </summary>
        /// <value>The items.</value>
        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenCheckBoxListItem<TValue>> items = new List<RadzenCheckBoxListItem<TValue>>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenCheckBoxListItem<TValue> item)
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
        public void RemoveItem(RadzenCheckBoxListItem<TValue> item)
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
        protected bool IsSelected(RadzenCheckBoxListItem<TValue> item)
        {
            return Value != null && Value.Contains(item.Value);
        }

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected async System.Threading.Tasks.Task SelectItem(RadzenCheckBoxListItem<TValue> item)
        {
            if (Disabled || item.Disabled || ReadOnly || item.ReadOnly)
                return;

            focusedIndex = allItems.IndexOf(item);

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

        bool HasInvisibleBefore(RadzenCheckBoxListItem<TValue> item)
        {
            return allItems.Take(allItems.IndexOf(item)).Any(t => !t.Visible && !t.Disabled);
        }

        bool IsFocused(RadzenCheckBoxListItem<TValue> item)
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
