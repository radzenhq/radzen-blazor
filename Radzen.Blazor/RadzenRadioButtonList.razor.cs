using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRadioButtonList component.
    /// Implements the <see cref="Radzen.FormComponent{TValue}" />
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="Radzen.FormComponent{TValue}" />
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
                                                                            .AddDisabled(Disabled || item.Disabled);

        ClassList IconClassList(RadzenRadioButtonListItem<TValue> item) => ClassList.Create("rz-radiobutton-icon")
                                                                            .Add("rzi rzi-circle-on", IsSelected(item));
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

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
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

        /// <summary>
        /// The items
        /// </summary>
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
    }
}