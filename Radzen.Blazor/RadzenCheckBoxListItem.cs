using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCheckBoxListItem component.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public partial class RadzenCheckBoxListItem<TValue> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public TValue Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenCheckBoxListItem{TValue}"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public virtual bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        RadzenCheckBoxList<TValue> _checkBoxList;

        /// <summary>
        /// Gets or sets the CheckBox list.
        /// </summary>
        /// <value>The CheckBox list.</value>
        [CascadingParameter]
        public RadzenCheckBoxList<TValue> CheckBoxList
        {
            get
            {
                return _checkBoxList;
            }
            set
            {
                if (_checkBoxList != value)
                {
                    _checkBoxList = value;
                    _checkBoxList.AddItem(this);
                }
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            CheckBoxList?.RemoveItem(this);
        }

        internal void SetText(string value)
        {
            Text = value;
        }

        internal void SetValue(TValue value)
        {
            Value = value;
        }

        internal void SetDisabled(bool value)
        {
            Disabled = value;
        }

        internal void SetReadOnly(bool value)
        {
            ReadOnly = value;
        }

        internal string GetItemId()
        {
            return GetId();
        }

        internal string GetItemCssClass()
        {
            return GetCssClass();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-checkbox";
        }
    }
}
