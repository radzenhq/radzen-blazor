using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenAccordion component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
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
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
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
                    if (!Multiple)
                    {
                        expandedIdexes.Clear();
                    }

                    if (!expandedIdexes.Contains(SelectedIndex))
                    {
                        expandedIdexes.Add(SelectedIndex);
                    }
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
                try { InvokeAsync(StateHasChanged); } catch { }
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
            return expandedIdexes.Contains(index);
        }

        /// <summary>
        /// The expanded idexes
        /// </summary>
        List<int> expandedIdexes = new List<int>();

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal async System.Threading.Tasks.Task SelectItem(RadzenAccordionItem item)
        {
            await CollapseAll(item);

            var itemIndex = items.IndexOf(item);
            if (!expandedIdexes.Contains(itemIndex))
            {
                expandedIdexes.Add(itemIndex);
                await Expand.InvokeAsync(itemIndex);
            }
            else
            {
                expandedIdexes.Remove(itemIndex);
                await Collapse.InvokeAsync(itemIndex);
            }

            if (!Multiple)
            {
                SelectedIndex = itemIndex;
            }

            StateHasChanged();
        }

        /// <summary>
        /// Collapses all items.
        /// </summary>
        /// <param name="item">The item.</param>
        async System.Threading.Tasks.Task CollapseAll(RadzenAccordionItem item)
        {
            if (!Multiple && items.Count > 1)
            {
                foreach (var i in items.Where(i => i != item))
                {
                    var itemIndex = items.IndexOf(i);
                    if (expandedIdexes.Contains(itemIndex))
                    {
                        expandedIdexes.Remove(itemIndex);
                        await Collapse.InvokeAsync(items.IndexOf(i));
                    }
                }
            }
        }
    }
}