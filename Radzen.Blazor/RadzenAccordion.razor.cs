using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
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
        public int SelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets the value changed.
        /// </summary>
        /// <value>The value changed.</value>
        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

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
                if (item.GetSelected())
                {
                    SelectedIndexChanged.InvokeAsync(items.Count);
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
                await SelectedIndexChanged.InvokeAsync(itemIndex);
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

        internal int focusedIndex = -1;
        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;

                focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowUp" ? -1 : 1), 0, items.Count - 1);
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < items.Count)
                {
                    await SelectItem(items.Where(i => i.Visible).ElementAt(focusedIndex));
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }

        internal bool IsFocused(RadzenAccordionItem item)
        {
            return items.Where(i => i.Visible).ToList().IndexOf(item) == focusedIndex && focusedIndex != -1;
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                var item = items.Where(i => i.Visible).ElementAtOrDefault(parameters.GetValueOrDefault<int>(nameof(SelectedIndex)));
                if (item != null && !item.GetSelected())
                {
                    await SelectItem(item);
                }
            }
            
            await base.SetParametersAsync(parameters);
        }
    }
}
