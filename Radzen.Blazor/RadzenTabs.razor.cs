using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenTabs.
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
        /// Gets or sets the index of the selected.
        /// </summary>
        /// <value>The index of the selected.</value>
        [Parameter]
        public int SelectedIndex { get; set; } = -1;

        /// <summary>
        /// The selected index
        /// </summary>
        private int selectedIndex = -1;

        /// <summary>
        /// Gets or sets the selected index changed.
        /// </summary>
        /// <value>The selected index changed.</value>
        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        [Parameter]
        public EventCallback<int> Change { get; set; }

        /// <summary>
        /// Gets or sets the tabs.
        /// </summary>
        /// <value>The tabs.</value>
        [Parameter]
        public RenderFragment Tabs { get; set; }

        /// <summary>
        /// The tabs
        /// </summary>
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

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        internal string Id
        {
            get
            {
                return GetId();
            }
        }

        /// <summary>
        /// Gets the selected tab.
        /// </summary>
        /// <value>The selected tab.</value>
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

        /// <summary>
        /// Determines whether the specified tab is selected.
        /// </summary>
        /// <param name="tab">The tab.</param>
        /// <returns><c>true</c> if the specified tab is selected; otherwise, <c>false</c>.</returns>
        internal bool IsSelected(RadzenTabsItem tab)
        {
            return IndexOf(tab) == selectedIndex;
        }

        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="tab">The tab.</param>
        /// <returns>System.Int32.</returns>
        internal int IndexOf(RadzenTabsItem tab)
        {
            return tabs.IndexOf(tab);
        }

        /// <summary>
        /// Selects the tab.
        /// </summary>
        /// <param name="tab">The tab.</param>
        /// <param name="raiseChange">if set to <c>true</c> [raise change].</param>
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
        /// Called when [initialized].
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

        /// <summary>
        /// The should render
        /// </summary>
        bool shouldRender = true;
        /// <summary>
        /// Shoulds the render.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ShouldRender()
        {
            return shouldRender;
        }

        /// <summary>
        /// Selects the tab on client.
        /// </summary>
        /// <param name="tab">The tab.</param>
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