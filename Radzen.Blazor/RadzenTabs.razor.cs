using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenTabs : RadzenComponent
    {
        [Parameter]
        public TabRenderMode RenderMode { get; set; } = TabRenderMode.Server;

        [Parameter]
        public int SelectedIndex { get; set; } = -1;

        private int selectedIndex = -1;

        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        [Parameter]
        public EventCallback<int> Change { get; set; }

        [Parameter]
        public RenderFragment Tabs { get; set; }

        List<RadzenTabsItem> tabs = new List<RadzenTabsItem>();

        public async Task AddTab(RadzenTabsItem tab)
        {
            if (!tabs.Contains(tab))
            {
                tabs.Add(tab);

                if (tab.Selected)
                {
                    selectedIndex = IndexOf(tab);
                }

                if (IsSelected(tab))
                {
                    await SelectTab(tab);
                }
                else if (selectedIndex < 0)
                {
                    await SelectTab(tab); // Select the first tab by default
                }
            }
        }

        internal string Id
        {
            get
            {
                return GetId();
            }
        }

        RadzenTabsItem SelectedTab
        {
            get
            {
                return tabs.ElementAtOrDefault(selectedIndex);
            }
        }

        public void RemoveItem(RadzenTabsItem item)
        {
            if (tabs.Contains(item))
            {
                tabs.Remove(item);

                try
                {
                    InvokeAsync(StateHasChanged);
                }
                catch
                {

                }
            }
        }

        public void Reload()
        {
            StateHasChanged();
        }

        internal bool IsSelected(RadzenTabsItem tab)
        {
            return IndexOf(tab) == selectedIndex;
        }

        internal int IndexOf(RadzenTabsItem tab)
        {
            return tabs.IndexOf(tab);
        }

        internal async Task SelectTab(RadzenTabsItem tab, bool raiseChange = false)
        {
            selectedIndex = IndexOf(tab);

            if (raiseChange)
            {
                await Change.InvokeAsync(selectedIndex);

                await SelectedIndexChanged.InvokeAsync(selectedIndex);
            }

            StateHasChanged();
        }

        protected override string GetComponentCssClass()
        {
            return "rz-tabview";
        }

        protected override void OnInitialized()
        {
            selectedIndex = SelectedIndex;

            base.OnInitialized();
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                selectedIndex = parameters.GetValueOrDefault<int>(nameof(SelectedIndex));
            }

            await base.SetParametersAsync(parameters);
        }

        bool shouldRender = true;
        protected override bool ShouldRender()
        {
            return shouldRender;
        }

        internal async System.Threading.Tasks.Task SelectTabOnClient(RadzenTabsItem tab)
        {
            var index = IndexOf(tab);
            if (index != selectedIndex)
            {
                selectedIndex = index;

                await JSRuntime.InvokeVoidAsync("Radzen.selectTab", $"{GetId()}-tabpanel-{selectedIndex}", selectedIndex);

                shouldRender = false;
                await Change.InvokeAsync(selectedIndex);
                await SelectedIndexChanged.InvokeAsync(selectedIndex);
                shouldRender = true;
            }
        }
    }
}