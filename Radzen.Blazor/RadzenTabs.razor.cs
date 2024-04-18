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
    /// RadzenTabs component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTabs RenderMode="TabRenderMode.Client" Change=@(args => Console.WriteLine($"Selected index is: {args}"))&gt;
    ///     &lt;Tabs&gt;
    ///         &lt;RadzenTabsItem Text="Orders"&gt;
    ///             Details for Orders
    ///         &lt;/RadzenTabsItem&gt;
    ///         &lt;RadzenTabsItem Text="Employees"&gt;
    ///             Details for Employees
    ///         &lt;/RadzenTabsItem&gt;
    ///     &lt;/Tabs&gt;
    /// &lt;/RadzenTabs&gt;
    /// </code>
    /// </example>
    public partial class RadzenTabs : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the render mode.
        /// </summary>
        /// <value>The render mode.</value>
        [Parameter]
        public TabRenderMode RenderMode { get; set; } = TabRenderMode.Server;

        /// <summary>
        /// Gets or sets the tab position.
        /// </summary>
        /// <value>The tab position.</value>
        [Parameter]
        public TabPosition TabPosition { get; set; } = TabPosition.Top;

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

            if (key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyPress = true;

                focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowLeft" ? -1 : 1), 0, tabs.Where(t => t.Visible).Count() - 1);
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;

                focusedIndex = key == "Home" ? 0 : tabs.Where(t => t.Visible).Count() - 1;
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < tabs.Where(t => t.Visible).Count())
                {
                    await tabs.Where(t => t.Visible).ToList()[focusedIndex].OnClick();
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }
        internal bool IsFocused(RadzenTabsItem item)
        {
            return tabs.Where(t => t.Visible).ToList().IndexOf(item) == focusedIndex && focusedIndex != -1;
        }
    }
}
