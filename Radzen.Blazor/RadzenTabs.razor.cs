using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tabbed interface component that organizes content into multiple panels with clickable tabs for navigation.
    /// RadzenTabs allows users to switch between different views or sections without navigating away from the page.
    /// Provides a container for RadzenTabsItem components, each representing one tab and its associated content panel.
    /// Supports tab positioning at Top, Bottom, Left, Right, TopRight, or BottomRight, server-side rendering (default) or client-side rendering for improved interactivity,
    /// programmatic selection via SelectedIndex with two-way binding, Change event when tabs are switched, dynamic tab addition/removal using AddTab() and RemoveItem(),
    /// keyboard navigation (Arrow keys, Home, End, Space, Enter) for accessibility, and disabled tabs to prevent selection.
    /// Use Server render mode for standard Blazor rendering, or Client mode for faster tab switching with JavaScript.
    /// </summary>
    /// <example>
    /// Basic tabs with server-side rendering:
    /// <code>
    /// &lt;RadzenTabs&gt;
    ///     &lt;Tabs&gt;
    ///         &lt;RadzenTabsItem Text="Orders"&gt;
    ///             Order list and details...
    ///         &lt;/RadzenTabsItem&gt;
    ///         &lt;RadzenTabsItem Text="Customers"&gt;
    ///             Customer information...
    ///         &lt;/RadzenTabsItem&gt;
    ///     &lt;/Tabs&gt;
    /// &lt;/RadzenTabs&gt;
    /// </code>
    /// Tabs with client-side rendering and change event:
    /// <code>
    /// &lt;RadzenTabs RenderMode="TabRenderMode.Client" @bind-SelectedIndex=@selectedTab Change=@OnTabChange&gt;
    ///     &lt;Tabs&gt;
    ///         &lt;RadzenTabsItem Text="Tab 1" Icon="home"&gt;Content 1&lt;/RadzenTabsItem&gt;
    ///         &lt;RadzenTabsItem Text="Tab 2" Icon="settings" Disabled="true"&gt;Content 2&lt;/RadzenTabsItem&gt;
    ///     &lt;/Tabs&gt;
    /// &lt;/RadzenTabs&gt;
    /// @code {
    ///     int selectedTab = 0;
    ///     void OnTabChange(int index) => Console.WriteLine($"Selected tab: {index}");
    /// }
    /// </code>
    /// </example>
    public partial class RadzenTabs : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the rendering mode that determines how tab content is rendered and switched.
        /// Server mode re-renders on the server when tabs change, while Client mode uses JavaScript for instant switching.
        /// </summary>
        /// <value>The tab render mode. Default is <see cref="TabRenderMode.Server"/>.</value>
        [Parameter]
        public TabRenderMode RenderMode { get; set; } = TabRenderMode.Server;

        /// <summary>
        /// Gets or sets the visual position of the tab headers relative to the content panels.
        /// Controls the layout direction and can position tabs at Top, Bottom, Left, Right, TopRight, or BottomRight of the content.
        /// </summary>
        /// <value>The tab position. Default is <see cref="TabPosition.Top"/>.</value>
        [Parameter]
        public TabPosition TabPosition { get; set; } = TabPosition.Top;

        /// <summary>
        /// Gets or sets the zero-based index of the currently selected tab.
        /// Use with @bind-SelectedIndex for two-way binding to track and control the active tab.
        /// Set to -1 for no selection (though typically the first tab is selected automatically).
        /// </summary>
        /// <value>The selected tab index. Default is -1 (auto-select first tab).</value>
        [Parameter]
        public int SelectedIndex { get; set; } = -1;

        private int selectedIndex = -1;

        /// <summary>
        /// Gets or sets the callback invoked when the selected tab index changes.
        /// Used for two-way binding with @bind-SelectedIndex.
        /// </summary>
        /// <value>The event callback receiving the new selected index.</value>
        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the user switches to a different tab.
        /// Provides the index of the newly selected tab. Use this for side effects or logging.
        /// </summary>
        /// <value>The change event callback receiving the selected tab index.</value>
        [Parameter]
        public EventCallback<int> Change { get; set; }

        /// <summary>
        /// Gets or sets the render fragment containing RadzenTabsItem components that define the tabs.
        /// Each RadzenTabsItem represents one tab with its header and content.
        /// </summary>
        /// <value>The tabs render fragment containing tab definitions.</value>
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

        /// <summary>
        /// Gets the currently selected RadzenTabsItem based on the selectedIndex.
        /// </summary>

        public RadzenTabsItem SelectedTab
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

                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
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

            SetFocusedIndex();

            if (raiseChange)
            {
                await Change.InvokeAsync(selectedIndex);

                await SelectedIndexChanged.InvokeAsync(selectedIndex);
            }

            StateHasChanged();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var positionCSS = "rz-tabview-top";

            if (TabPosition == TabPosition.Bottom)
            {
                positionCSS = "rz-tabview-bottom";
            }
            else if (TabPosition == TabPosition.Right)
            {
                positionCSS = "rz-tabview-right";
            }
            else if (TabPosition == TabPosition.Left)
            {
                positionCSS = "rz-tabview-left";
            }
            else if(TabPosition == TabPosition.TopRight)
            {
                positionCSS = "rz-tabview-top rz-tabview-top-right";
            }
            else if (TabPosition == TabPosition.BottomRight)
            {
                positionCSS = "rz-tabview-bottom rz-tabview-bottom-right";
            }

            return $"rz-tabview {positionCSS}";
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            selectedIndex = SelectedIndex;

            focusedIndex = focusedIndex == -1 ? 0 : focusedIndex;

            base.OnInitialized();
        }

        void SetFocusedIndex()
        {
            if (focusedIndex != selectedIndex)
            {
                focusedIndex = selectedIndex;
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                selectedIndex = parameters.GetValueOrDefault<int>(nameof(SelectedIndex));
            }

            SetFocusedIndex();

            await base.SetParametersAsync(parameters);
        }


        int previousSelectedIndex;
        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (RenderMode == TabRenderMode.Client && previousSelectedIndex != selectedIndex)
            {
                previousSelectedIndex = selectedIndex;
                await JSRuntime.InvokeVoidAsync("Radzen.selectTab", $"{GetId()}-tabpanel-{selectedIndex}", selectedIndex);
            }

            await base.OnAfterRenderAsync(firstRender);
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
                previousSelectedIndex = selectedIndex;
                SetFocusedIndex();

                await JSRuntime.InvokeVoidAsync("Radzen.selectTab", $"{GetId()}-tabpanel-{selectedIndex}", selectedIndex);

                shouldRender = false;
                await Change.InvokeAsync(selectedIndex);
                await SelectedIndexChanged.InvokeAsync(selectedIndex);
                shouldRender = true;
            }
        }

        internal RadzenTabsItem FirstVisibleTab()
        {
            return tabs.Where(t => t.Visible).FirstOrDefault();
        }

        internal int focusedIndex = -1;
        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            var item = tabs.ElementAtOrDefault(focusedIndex) ?? tabs.FirstOrDefault();

            if (item == null) return;

            if (key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyPress = true;

                focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowLeft" ? -1 : 1), 0, tabs.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count() - 1);
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;

                focusedIndex = key == "Home" ? 0 : tabs.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count() - 1;
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < tabs.Where(t => HasInvisibleBefore(item) ? true : t.Visible).Count())
                {
                    await tabs.Where(t => HasInvisibleBefore(item) ? true : t.Visible).ToList()[focusedIndex].OnClick();
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }
        internal bool IsFocused(RadzenTabsItem item)
        {
            return tabs.Where(t => HasInvisibleBefore(item) ? true : t.Visible).ToList().IndexOf(item) == focusedIndex && focusedIndex != -1;
        }

        internal bool HasInvisibleBefore(RadzenTabsItem item)
        {
            return tabs.Take(tabs.IndexOf(item)).Any(t => !t.Visible);
        }
    }
}
