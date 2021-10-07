using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenSelectBarItem.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public class RadzenSelectBarItem : RadzenComponent
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
        public object Value { get; set; }

        /// <summary>
        /// The select bar
        /// </summary>
        IRadzenSelectBar _selectBar;

        /// <summary>
        /// Gets or sets the select bar.
        /// </summary>
        /// <value>The select bar.</value>
        [CascadingParameter]
        public IRadzenSelectBar SelectBar
        {
            get
            {
                return _selectBar;
            }
            set
            {
                if (_selectBar != value)
                {
                    _selectBar = value;
                    _selectBar.AddItem(this);
                }
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            SelectBar?.RemoveItem(this);
        }

        /// <summary>
        /// Sets the text.
        /// </summary>
        /// <param name="value">The value.</param>
        internal void SetText(string value)
        {
            Text = value;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="value">The value.</param>
        internal void SetValue(object value)
        {
            Value = value;
        }
    }
}