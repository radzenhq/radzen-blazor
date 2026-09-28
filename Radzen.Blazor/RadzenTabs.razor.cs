using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
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
        /// Gets or sets the callback invoked before the selected tab changes in response to user interaction.
        /// Invoke the <see cref="TabsCanChangeEventArgs.PreventDefault"/> method to cancel the tab change - for example to guard unsaved changes.
        /// The callback can be asynchronous e.g. await a confirmation dialog before deciding.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenTabs CanChange=@OnCanChange&gt;
        /// &lt;/RadzenTabs&gt;
        /// @code {
        ///  async Task OnCanChange(TabsCanChangeEventArgs args)
        ///  {
        ///    if (hasUnsavedChanges &amp;&amp; await DialogService.Confirm("Discard unsaved changes?") != true)
        ///    {
        ///        args.PreventDefault();
        ///    }
        ///  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<TabsCanChangeEventArgs> CanChange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user can reorder tabs by dragging and dropping.
        /// When enabled, tab headers become draggable and can be rearranged by the user.
        /// </summary>
        /// <value><c>true</c> if tab reordering is allowed; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool AllowReorder { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when tabs are reordered via drag and drop.
        /// Provides a <see cref="TabsReorderEventArgs"/> with the old and new index of the moved tab.
        /// </summary>
        [Parameter]
        public EventCallback<TabsReorderEventArgs> Reorder { get; set; }

        /// <summary>
        /// Gets or sets the render fragment containing RadzenTabsItem components that define the tabs.
        /// Each RadzenTabsItem represents one tab with its header and content.
        /// </summary>
        /// <value>The tabs render fragment containing tab definitions.</value>
        [Parameter]
        public RenderFragment? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the accessible name applied to the tab list via <c>aria-label</c>.
        /// Use this to give assistive technologies a label describing the group of tabs.
        /// </summary>
        /// <value>The tab list aria-label. Default is <c>null</c>.</value>
        [Parameter]
        public string? AriaLabel { get; set; }

        /// <summary>
        /// Gets or sets the id of the element that labels the tab list via <c>aria-labelledby</c>.
        /// Use this when a visible element already provides the accessible name for the group of tabs.
        /// </summary>
        /// <value>The id of the labelling element. Default is <c>null</c>.</value>
        [Parameter]
        public string? AriaLabelledBy { get; set; }

        /// <summary>
        /// Gets or sets how tab titles that do not fit in the available space are laid out.
        /// <see cref="TabsOverflow.Default"/> shrinks the titles to their minimum size and lets the tab list overflow, <see cref="TabsOverflow.Wrap"/> wraps them to additional lines, <see cref="TabsOverflow.Scroll"/> scrolls the tab list
        /// and <see cref="TabsOverflow.Collapse"/> moves the titles that do not fit into a dropdown menu opened from a button at the end of the tab list. The selected tab always stays visible.
        /// </summary>
        /// <value>The overflow mode. Default is <see cref="TabsOverflow.Default"/>.</value>
        [Parameter]
        public TabsOverflow Overflow { get; set; } = TabsOverflow.Default;

        string? moreAriaLabel;

        /// <summary>
        /// Gets or sets the accessible name of the button that opens the menu with the collapsed tabs when <see cref="Overflow"/> is <see cref="TabsOverflow.Collapse"/>.
        /// </summary>
        /// <value>The accessible name of the button. Default is the localized "More tabs".</value>
        [Parameter]
        public string MoreAriaLabel { get => moreAriaLabel ?? Localize(nameof(RadzenStrings.Tabs_MoreAriaLabel)); set => moreAriaLabel = value; }

        string? scrollBackwardAriaLabel;

        /// <summary>
        /// Gets or sets the accessible name of the button that scrolls the tab list towards its start when <see cref="Overflow"/> is <see cref="TabsOverflow.Scroll"/>.
        /// </summary>
        /// <value>The accessible name of the button. Default is the localized "Scroll tabs backward".</value>
        [Parameter]
        public string ScrollBackwardAriaLabel { get => scrollBackwardAriaLabel ?? Localize(nameof(RadzenStrings.Tabs_ScrollBackwardAriaLabel)); set => scrollBackwardAriaLabel = value; }

        string? scrollForwardAriaLabel;

        /// <summary>
        /// Gets or sets the accessible name of the button that scrolls the tab list towards its end when <see cref="Overflow"/> is <see cref="TabsOverflow.Scroll"/>.
        /// </summary>
        /// <value>The accessible name of the button. Default is the localized "Scroll tabs forward".</value>
        [Parameter]
        public string ScrollForwardAriaLabel { get => scrollForwardAriaLabel ?? Localize(nameof(RadzenStrings.Tabs_ScrollForwardAriaLabel)); set => scrollForwardAriaLabel = value; }

        internal ElementReference tablistElement;

        List<RadzenTabsItem> tabs = new List<RadzenTabsItem>();

        internal List<RadzenTabsItem> NavigableTabs()
        {
            var item = tabs.ElementAtOrDefault(selectedIndex) ?? tabs.FirstOrDefault();

            if (item == null)
            {
                return new List<RadzenTabsItem>();
            }

            return tabs.Where(t => HasInvisibleBefore(item) ? true : t.Visible).ToList();
        }

        internal string? GetActiveDescendantId()
        {
            if (focusedIndex < 0)
            {
                return null;
            }

            var focused = NavigableTabs().ElementAtOrDefault(focusedIndex);
            if (focused == null)
            {
                return null;
            }

            return $"{GetId()}-tabpanel-{IndexOf(focused)}-label";
        }

        /// <summary>
        /// Adds the tab.
        /// </summary>
        /// <param name="tab">The tab.</param>
        public async Task AddTab(RadzenTabsItem tab)
        {
            ArgumentNullException.ThrowIfNull(tab);
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

        internal string? Id
        {
            get
            {
                return GetId();
            }
        }

        /// <summary>
        /// Gets the currently selected RadzenTabsItem based on the selectedIndex.
        /// </summary>

        public RadzenTabsItem? SelectedTab
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
            if (tabs.Remove(item))
            {
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
            var newIndex = IndexOf(tab);

            if (raiseChange && newIndex != selectedIndex)
            {
                var canChangeArgs = new TabsCanChangeEventArgs { SelectedIndex = selectedIndex, NewIndex = newIndex };

                await CanChange.InvokeAsync(canChangeArgs);

                if (canChangeArgs.IsDefaultPrevented)
                {
                    return;
                }
            }

            selectedIndex = newIndex;

            SetFocusedIndex();

            try
            {
                if (raiseChange)
                {
                    await Change.InvokeAsync(selectedIndex);

                    await SelectedIndexChanged.InvokeAsync(selectedIndex);

                    try
                    {
                        await tablistElement.FocusAsync(preventScroll: true);
                    }
                    catch (JSDisconnectedException)
                    {
                    }
                    catch (JSException)
                    {
                    }
                }
            }
            finally
            {
                StateHasChanged();
            }
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

            var overflowCSS = Overflow switch
            {
                TabsOverflow.Wrap => " rz-tabview-overflow-wrap",
                TabsOverflow.Scroll => " rz-tabview-overflow-scroll",
                TabsOverflow.Collapse => " rz-tabview-overflow-collapse",
                _ => ""
            };

            return $"rz-tabview {positionCSS}{overflowCSS}";
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            selectedIndex = SelectedIndex;

            SetFocusedIndex();

            if (focusedIndex == -1)
            {
                focusedIndex = 0;
            }

            base.OnInitialized();
        }

        void SetFocusedIndex()
        {
            var selected = tabs.ElementAtOrDefault(selectedIndex);

            if (selected != null)
            {
                var navigableIndex = NavigableTabs().IndexOf(selected);

                if (navigableIndex != -1)
                {
                    focusedIndex = navigableIndex;
                }
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                selectedIndex = parameters.GetValueOrDefault<int>(nameof(SelectedIndex));
            }

            overflowChanged |= parameters.DidParameterChange(nameof(Overflow), Overflow);
            positionChanged |= parameters.DidParameterChange(nameof(TabPosition), TabPosition);
            visibleChanged |= parameters.DidParameterChange(nameof(Visible), Visible);

            SetFocusedIndex();

            await base.SetParametersAsync(parameters);

            if (overflowChanged || positionChanged || (visibleChanged && !Visible))
            {
                DisposeOverflow();
            }
        }


        int previousSelectedIndex;
        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (JSRuntime != null && RenderMode == TabRenderMode.Client && previousSelectedIndex != selectedIndex)
            {
                previousSelectedIndex = selectedIndex;
                try
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.selectTab", $"{GetId()}-tabpanel-{selectedIndex}", selectedIndex);
                }
                catch (JSDisconnectedException)
                {
                }
            }

            if ((Overflow == TabsOverflow.Collapse || Overflow == TabsOverflow.Scroll) && Visible && JSRuntime != null && (firstRender || overflowChanged || positionChanged || visibleChanged))
            {
                overflowChanged = false;
                positionChanged = false;
                visibleChanged = false;

                var version = ++_jsRefVersion;
                var jsRef = _jsRef;
                _jsRef = null;

                try
                {
                    if (jsRef != null)
                    {
                        await jsRef.InvokeVoidAsync("dispose");
                        await jsRef.DisposeAsync();
                    }

                    if (version == _jsRefVersion)
                    {
                        var created = Overflow == TabsOverflow.Collapse
                            ? await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createTabsOverflow", tablistElement, Reference, MorePopupId, MoreButtonId)
                            : await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createTabsScroll", tablistElement);

                        if (version == _jsRefVersion)
                        {
                            _jsRef = created;
                        }
                        else if (created != null)
                        {
                            await created.InvokeVoidAsync("dispose");
                            await created.DisposeAsync();
                        }
                    }
                }
                catch (JSDisconnectedException)
                {
                }
            }
            else
            {
                overflowChanged = false;
                positionChanged = false;
                visibleChanged = false;
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        bool shouldRender = true;
        bool suppressNextRender;
        /// <summary>
        /// Should render.
        /// </summary>
        protected override bool ShouldRender()
        {
            if (suppressNextRender)
            {
                suppressNextRender = false;
                return false;
            }

            return shouldRender;
        }

        internal async System.Threading.Tasks.Task SelectTabOnClient(RadzenTabsItem tab)
        {
            var index = IndexOf(tab);
            if (index != selectedIndex && JSRuntime != null)
            {
                var canChangeArgs = new TabsCanChangeEventArgs { SelectedIndex = selectedIndex, NewIndex = index };

                await CanChange.InvokeAsync(canChangeArgs);

                if (canChangeArgs.IsDefaultPrevented)
                {
                    return;
                }

                selectedIndex = index;
                previousSelectedIndex = selectedIndex;
                SetFocusedIndex();

                try
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.selectTab", $"{GetId()}-tabpanel-{selectedIndex}", selectedIndex);
                }
                catch (JSDisconnectedException)
                {
                }

                shouldRender = false;
                try
                {
                    await Change.InvokeAsync(selectedIndex);
                    await SelectedIndexChanged.InvokeAsync(selectedIndex);
                }
                finally
                {
                    shouldRender = true;
                }

                try
                {
                    await tablistElement.FocusAsync(preventScroll: true);
                }
                catch (JSDisconnectedException)
                {
                }
                catch (JSException)
                {
                }
            }
        }

        internal ElementReference moreButtonElement;

        IJSObjectReference? _jsRef;
        int _jsRefVersion;
        bool overflowChanged;
        bool positionChanged;
        bool visibleChanged;
        bool moreOpen;

        double overflowAvailable;
        double overflowMoreSize;
        int[] overflowIndexes = Array.Empty<int>();
        double[] overflowSizes = Array.Empty<double>();
        readonly HashSet<int> collapsedTabs = new HashSet<int>();

        string MorePopupId => $"popup{GetId()}-more";

        string MoreButtonId => $"{GetId()}-more";

        string MoreMenuId => $"{GetId()}-more-menu";

        string MoreItemId(RadzenTabsItem tab) => $"{GetId()}-more-item-{IndexOf(tab)}";

        string MoreClass => ClassList.Create("rz-tabview-more")
                                     .Add("rz-tabview-collapsed", collapsedTabs.Count == 0)
                                     .ToString();

        string MoreItemClass(RadzenTabsItem tab) => ClassList.Create("rz-tabview-more-item")
                                                             .AddDisabled(tab.Disabled)
                                                             .ToString();

        internal bool IsCollapsed(RadzenTabsItem tab)
        {
            return Overflow == TabsOverflow.Collapse && collapsedTabs.Contains(IndexOf(tab));
        }

        internal int? GetAriaSetSize()
        {
            return Overflow == TabsOverflow.Collapse ? tabs.Count(t => t.Visible) : null;
        }

        internal int? GetAriaPosInSet(RadzenTabsItem tab)
        {
            if (Overflow != TabsOverflow.Collapse)
            {
                return null;
            }

            var position = 0;

            foreach (var item in tabs)
            {
                if (item.Visible)
                {
                    position++;
                }

                if (item == tab)
                {
                    return item.Visible ? position : null;
                }
            }

            return null;
        }

        IEnumerable<RadzenTabsItem> CollapsedTabs()
        {
            foreach (var index in overflowIndexes)
            {
                if (collapsedTabs.Contains(index) && index < tabs.Count)
                {
                    yield return tabs[index];
                }
            }
        }

        void UpdateCollapsedTabs()
        {
            collapsedTabs.Clear();

            if (Overflow != TabsOverflow.Collapse || overflowIndexes.Length == 0)
            {
                return;
            }

            var items = new List<(int Index, double Size)>();
            var total = 0d;

            for (var i = 0; i < overflowIndexes.Length && i < overflowSizes.Length; i++)
            {
                var index = overflowIndexes[i];

                if (index >= 0 && index < tabs.Count && tabs[index].Visible)
                {
                    items.Add((index, overflowSizes[i]));
                    total += overflowSizes[i];
                }
            }

            if (total <= overflowAvailable + 1)
            {
                return;
            }

            var focused = NavigableTabs().ElementAtOrDefault(focusedIndex);
            var focusedTabIndex = focused != null ? IndexOf(focused) : -1;

            bool IsPinned(int index) => index == selectedIndex || index == focusedTabIndex;

            var used = items.Where(item => IsPinned(item.Index)).Sum(item => item.Size);
            var budget = overflowAvailable - overflowMoreSize + 1;
            var fits = true;

            foreach (var (index, size) in items)
            {
                if (IsPinned(index))
                {
                    continue;
                }

                if (fits && used + size <= budget)
                {
                    used += size;
                }
                else
                {
                    fits = false;
                    collapsedTabs.Add(index);
                }
            }
        }

        /// <summary>
        /// Invoked from JavaScript with the size of the tab list and of every tab when <see cref="Overflow"/> is <see cref="TabsOverflow.Collapse"/>.
        /// </summary>
        /// <param name="available">The size of the tab list along its main axis.</param>
        /// <param name="moreSize">The size of the button that opens the menu with the collapsed tabs.</param>
        /// <param name="indexes">The indexes of the tabs in visual order.</param>
        /// <param name="sizes">The size of every tab in the order of <paramref name="indexes"/>.</param>
        [JSInvokable("OnTabsOverflow")]
        public async Task OnTabsOverflow(double available, double moreSize, int[] indexes, double[] sizes)
        {
            overflowAvailable = available;
            overflowMoreSize = moreSize;
            overflowIndexes = indexes ?? Array.Empty<int>();
            overflowSizes = sizes ?? Array.Empty<double>();

            UpdateCollapsedTabs();

            if (moreOpen && collapsedTabs.Count == 0)
            {
                await CloseMore(restoreFocus: true);
            }

            StateHasChanged();
        }

        /// <summary>
        /// Invoked from JavaScript when the menu with the collapsed tabs is closed client-side, e.g. by clicking outside of it.
        /// </summary>
        [JSInvokable("OnMorePopupClose")]
        public void OnMorePopupClose()
        {
            moreOpen = false;
            StateHasChanged();
        }

        async Task ToggleMore()
        {
            if (moreOpen)
            {
                await CloseMore();
                return;
            }

            moreOpen = true;

            if (JSRuntime != null)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.openPopup", moreButtonElement, MorePopupId, false, null, null, null, Reference, nameof(OnMorePopupClose));

                    var first = CollapsedTabs().FirstOrDefault(tab => !tab.Disabled);

                    if (first != null)
                    {
                        await JSRuntime.InvokeVoidAsync("Radzen.focusElement", MoreItemId(first));
                    }
                }
                catch (JSDisconnectedException)
                {
                }
            }
        }

        async Task CloseMore(bool restoreFocus = false)
        {
            moreOpen = false;

            if (JSRuntime != null)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.closePopup", MorePopupId, null, null, null, !restoreFocus);
                }
                catch (JSDisconnectedException)
                {
                }
            }
        }

        async Task SelectCollapsedTab(RadzenTabsItem tab)
        {
            if (tab.Disabled)
            {
                return;
            }

            await CloseMore();

            await tab.OnClick();
        }

        void DisposeOverflow()
        {
            _jsRefVersion++;
            var jsRef = _jsRef;
            _jsRef = null;

            overflowIndexes = Array.Empty<int>();
            overflowSizes = Array.Empty<double>();
            collapsedTabs.Clear();

            if (jsRef == null)
            {
                return;
            }

            jsRef.InvokeVoid("dispose");
            jsRef.DisposeFireAndForget();

            if (moreOpen)
            {
                moreOpen = false;
                JSRuntime?.InvokeVoid("Radzen.closePopup", MorePopupId);
            }

            JSRuntime?.InvokeVoid("Radzen.destroyPopup", MorePopupId);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            DisposeOverflow();
        }

        internal RadzenTabsItem? FirstVisibleTab()
        {
            return tabs?.Where(t => t.Visible).FirstOrDefault();
        }

        internal bool IsVertical => TabPosition == TabPosition.Left || TabPosition == TabPosition.Right;

        internal int focusedIndex = -1;
        bool preventKeyPress = true;
        bool stopKeydownPropagation;

        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            var navigableTabs = NavigableTabs();

            if (navigableTabs.Count == 0)
            {
                return;
            }

            var previousKey = IsVertical ? "ArrowUp" : "ArrowLeft";
            var nextKey = IsVertical ? "ArrowDown" : "ArrowRight";

            if (key == previousKey || key == nextKey)
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                var direction = key == previousKey ? -1 : 1;
                var count = navigableTabs.Count;

                focusedIndex = ((focusedIndex + direction) % count + count) % count;

                var steps = 0;
                while (navigableTabs.ElementAtOrDefault(focusedIndex)?.Disabled == true && steps < count)
                {
                    focusedIndex = ((focusedIndex + direction) % count + count) % count;
                    steps++;
                }
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                var count = navigableTabs.Count;
                var direction = key == "Home" ? 1 : -1;

                focusedIndex = key == "Home" ? 0 : count - 1;

                var steps = 0;
                while (navigableTabs.ElementAtOrDefault(focusedIndex)?.Disabled == true && steps < count)
                {
                    focusedIndex = ((focusedIndex + direction) % count + count) % count;
                    steps++;
                }
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                if (focusedIndex >= 0 && focusedIndex < navigableTabs.Count)
                {
                    await navigableTabs[focusedIndex].OnClick();
                }
            }
            else
            {
                if (!preventKeyPress && !stopKeydownPropagation)
                {
                    suppressNextRender = true;
                }
                preventKeyPress = false;
                stopKeydownPropagation = false;
            }
        }
        internal bool IsFocused(RadzenTabsItem item)
        {
            return NavigableTabs().IndexOf(item) == focusedIndex && focusedIndex != -1;
        }

        internal bool HasInvisibleBefore(RadzenTabsItem item)
        {
            return tabs.Take(tabs.IndexOf(item)).Any(t => !t.Visible);
        }

        internal RadzenTabsItem? draggedTab;
        internal RadzenTabsItem? dragOverTab;

        internal void OnTabDragStart(RadzenTabsItem tab)
        {
            draggedTab = tab;
        }

        internal void OnTabDragOver(RadzenTabsItem tab)
        {
            dragOverTab = tab;
        }

        internal void OnTabDragEnd()
        {
            draggedTab = null;
            dragOverTab = null;
        }

        internal async Task OnTabDrop(RadzenTabsItem tab)
        {
            if (draggedTab == null || draggedTab == tab)
            {
                return;
            }

            var oldIndex = tabs.IndexOf(draggedTab);
            var newIndex = tabs.IndexOf(tab);

            if (oldIndex < 0 || newIndex < 0)
            {
                return;
            }

            tabs.RemoveAt(oldIndex);
            tabs.Insert(newIndex, draggedTab);

            // Adjust selectedIndex to follow the selected tab
            if (selectedIndex == oldIndex)
            {
                selectedIndex = newIndex;
            }
            else if (oldIndex < selectedIndex && newIndex >= selectedIndex)
            {
                selectedIndex--;
            }
            else if (oldIndex > selectedIndex && newIndex <= selectedIndex)
            {
                selectedIndex++;
            }

            SetFocusedIndex();

            await Reorder.InvokeAsync(new TabsReorderEventArgs { OldIndex = oldIndex, NewIndex = newIndex });
            await SelectedIndexChanged.InvokeAsync(selectedIndex);

            draggedTab = null;
            dragOverTab = null;

            StateHasChanged();
        }

        internal bool IsDragOver(RadzenTabsItem tab)
        {
            return dragOverTab == tab && draggedTab != tab;
        }
    }
}
