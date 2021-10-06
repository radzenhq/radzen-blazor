using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    public partial class RadzenAccordion : RadzenComponent
    {
        protected override string GetComponentCssClass()
        {
            return "rz-accordion";
        }

        [Parameter]
        public bool Multiple { get; set; }

        [Parameter]
        public int SelectedIndex { get; set; } = 0;

        [Parameter]
        public EventCallback<int> Expand { get; set; }

        [Parameter]
        public EventCallback<int> Collapse { get; set; }

        [Parameter]
        public RenderFragment Items { get; set; }

        List<RadzenAccordionItem> items = new List<RadzenAccordionItem>();

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

        public void RemoveItem(RadzenAccordionItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        internal void Refresh()
        {
            StateHasChanged();
        }

        protected bool IsSelected(int index, RadzenAccordionItem item)
        {
            return expandedIdexes.Contains(index);
        }

        List<int> expandedIdexes = new List<int>();

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