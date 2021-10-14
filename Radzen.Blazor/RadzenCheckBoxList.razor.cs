using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCheckBoxList component.
    /// Implements the <see cref="Radzen.FormComponent{IEnumerable{TValue}}" />
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <seealso cref="Radzen.FormComponent{IEnumerable{TValue}}" />
    /// <example>
    /// <code>
    /// &lt;RadzenCheckBoxList @bind-Value=@checkedValues TValue="int" &gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenCheckBoxListItem Text="Orders" Value="1" /&gt;
    ///         &lt;RadzenCheckBoxListItem Text="Employees" Value="2" /&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenCheckBoxList&gt;
    /// </code>
    /// </example>
    public partial class RadzenCheckBoxList<TValue> : FormComponent<IEnumerable<TValue>>
    {
        ClassList ItemClassList(RadzenCheckBoxListItem<TValue> item) => ClassList.Create("rz-chkbox-box")
                                                                            .Add("rz-state-active", IsSelected(item))
                                                                            .AddDisabled(Disabled || item.Disabled);

        ClassList IconClassList(RadzenCheckBoxListItem<TValue> item) => ClassList.Create("rz-chkbox-icon")
                                                                            .Add("rzi rzi-check", IsSelected(item));

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
        /// <summary>
        /// Gets or sets the data used to generate items.
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
            return GetClassList(Orientation == Orientation.Horizontal ? "rz-checkbox-list-horizontal" : "rz-checkbox-list-vertical").ToString();
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
                try { InvokeAsync(StateHasChanged); } catch { }
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

        /// <summary>
        /// Gets the state of the disabled.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string getDisabledState(RadzenCheckBoxListItem<TValue> item)
        {
            return Disabled || item.Disabled ? " rz-state-disabled" : "";
        }
    }
}