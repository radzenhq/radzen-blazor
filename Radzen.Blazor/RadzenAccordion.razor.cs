using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenAccordion component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenAccordion&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenAccordionItem Text="Orders" Icon="account_balance_wallet"&gt;
    ///             Details for Orders
    ///         &lt;/RadzenAccordionItem&gt;
    ///         &lt;RadzenAccordionItem Text="Employees" Icon="account_box"&gt;
    ///             Details for Employees
    ///         &lt;/RadzenAccordionItem&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenAccordion&gt;
    /// </code>
    /// </example>
    public partial class RadzenAccordion : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-accordion";
        }

        /// <summary>
        /// Gets or sets a value indicating whether multiple items can be expanded.
        /// </summary>
        /// <value><c>true</c> if multiple items can be expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets the index of the selected item.
        /// </summary>
        /// <value>The index of the selected item.</value>
        [Parameter]
        public int SelectedIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets a callback raised when the item is expanded.
        /// </summary>
        /// <value>The expand.</value>
        [Parameter]
        public EventCallback<int> Expand { get; set; }

        /// <summary>
        /// Gets or sets a callback raised when the item is collapsed.
        /// </summary>
        /// <value>The collapse.</value>
        [Parameter]
        public EventCallback<int> Collapse { get; set; }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenAccordionItem> items = new List<RadzenAccordionItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenAccordionItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                if (item.Selected)
                {
                    SelectedIndex = items.Count;
                }

                items.Add(item);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveItem(RadzenAccordionItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        internal void Refresh()
        {
            StateHasChanged();
        }

        /// <summary>
        /// Determines whether the specified index is selected.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the specified index is selected; otherwise, <c>false</c>.</returns>
        protected bool IsSelected(int index, RadzenAccordionItem item)
        {
            return item.GetSelected() == true;
        }

        /// <summary>
        /// Gets the item's title attribute value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>The item's collapse or expand title value depending on if the item is expanded or collapsed.
        /// If the relevant title is null or whitespace this method returns "Expand" or "Collapse".</returns>
        protected string ItemTitle(int index, RadzenAccordionItem item)
        {
            if (IsSelected(index, item))
            {
                return string.IsNullOrWhiteSpace(item.CollapseTitle) ? "Collapse" : item.CollapseTitle;
            }
            return string.IsNullOrWhiteSpace(item.ExpandTitle) ? "Expand" : item.ExpandTitle;
        }

        /// <summary>
        /// Gets the item's aria-label attribute value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>The item's collapse or expand aria-label value depending on if the item is expanded or collapsed.
        /// If the relevant aria-label is null or whitespace this method returns "Expand" or "Collapse".</returns>
        protected string ItemAriaLabel(int index, RadzenAccordionItem item)
        {
            if (IsSelected(index, item))
            {
                return string.IsNullOrWhiteSpace(item.CollapseAriaLabel) ? "Collapse" : item.CollapseAriaLabel;
            }
            return string.IsNullOrWhiteSpace(item.ExpandAriaLabel) ? "Expand" : item.ExpandAriaLabel;          
        }
        
        internal async System.Threading.Tasks.Task SelectItem(RadzenAccordionItem item, bool? value = null)
        {
            await CollapseAll(item);

            var itemIndex = items.IndexOf(item);

            var selected = item.GetSelected();

            if (selected)
            {
                await Collapse.InvokeAsync(itemIndex);
            }
            else
            {
                await Expand.InvokeAsync(itemIndex);
            }

            item.SetSelected(value ?? !selected);

            if (!Multiple)
            {
                SelectedIndex = itemIndex;
            }

            StateHasChanged();
        }

        async System.Threading.Tasks.Task CollapseAll(RadzenAccordionItem item)
        {
            if (!Multiple && items.Count > 1)
            {
                foreach (var i in items.Where(i => i != item))
                {
                    if (i.GetSelected())
                    {
                        i.SetSelected(false);
                        await Collapse.InvokeAsync(items.IndexOf(i));
                    }
                }
            }
        }

        bool preventKeyPress = false;
        async Task OnKeyPress(KeyboardEventArgs args, RadzenAccordionItem item)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                await SelectItem(item);
            }
            else
            {
                preventKeyPress = false;
            }
        }
    }
}
