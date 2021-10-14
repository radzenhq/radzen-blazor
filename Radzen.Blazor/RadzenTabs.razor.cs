using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTabs component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenTabs : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the render mode.
        /// </summary>
        /// <value>The render mode.</value>
        [Parameter]
        public TabRenderMode RenderMode { get; set; } = TabRenderMode.Server;

        /// <summary>
        /// Gets or sets the selected index.
        /// </summary>
        /// <value>The selected index.</value>
        [Parameter]
        public int SelectedIndex { get; set; } = -1;

        private int selectedIndex = -1;

        /// <summary>
        /// Gets or sets the selected index changed callback.
        /// </summary>
        /// <value>The selected index changed callback.</value>
        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        /// <summary>
        /// Gets or sets the change callback.
        /// </summary>
        /// <value>The change callback.</value>
        [Parameter]
        public EventCallback<int> Change { get; set; }

        /// <summary>
        /// Gets or sets the tabs.
        /// </summary>
        /// <value>The tabs.</value>
        [Parameter]
        public RenderFragment Tabs { get; set; }

        List<RadzenTabsItem> tabs = new List<RadzenTabsItem>();

        /// <summary>
        /// Adds the tab.
        /// </summary>
        /// <param name="tab">The tab.</param>
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

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
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

        /// <summary>
        /// Reloads this instance.
        /// </summary>
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

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-tabview";
        }

        /// <summary>
        /// Called when initialized.
        /// </summary>
        protected override void OnInitialized()
        {
            selectedIndex = SelectedIndex;

            base.OnInitialized();
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                selectedIndex = parameters.GetValueOrDefault<int>(nameof(SelectedIndex));
            }

            await base.SetParametersAsync(parameters);
        }

        bool shouldRender = true;
        /// <summary>
        /// Should render.
        /// </summary>
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