using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A horizontal menu component with support for nested submenus, icons, and responsive behavior.
    /// RadzenMenu provides a classic menu bar for navigation, typically used in application headers or toolbars.
    /// Displays menu items horizontally with dropdown submenus.
    /// Supports multi-level nested menus via RadzenMenuItem child items, automatic navigation via Path property or custom Click handlers,
    /// icons displayed alongside menu item text, responsive design that automatically collapses to a hamburger menu on small screens (configurable),
    /// click-to-open or hover-to-open interaction modes, keyboard navigation (Arrow keys, Enter, Escape) for accessibility, and visual separators between menu items.
    /// Use for application navigation bars, command menus, or toolbar-style interfaces. Menu items are defined using RadzenMenuItem components as child content.
    /// </summary>
    /// <example>
    /// Basic menu with navigation:
    /// <code>
    /// &lt;RadzenMenu&gt;
    ///     &lt;RadzenMenuItem Text="Home" Path="/" Icon="home" /&gt;
    ///     &lt;RadzenMenuItem Text="Data"&gt;
    ///         &lt;RadzenMenuItem Text="Orders" Path="/orders" /&gt;
    ///         &lt;RadzenMenuItem Text="Customers" Path="/customers" /&gt;
    ///     &lt;/RadzenMenuItem&gt;
    ///     &lt;RadzenMenuItem Text="Reports" Path="/reports" /&gt;
    /// &lt;/RadzenMenu&gt;
    /// </code>
    /// Menu with click handlers:
    /// <code>
    /// &lt;RadzenMenu Click=@OnMenuClick&gt;
    ///     &lt;RadzenMenuItem Text="File"&gt;
    ///         &lt;RadzenMenuItem Text="New" Value="new" Icon="add" /&gt;
    ///         &lt;RadzenMenuItem Text="Open" Value="open" Icon="folder_open" /&gt;
    ///         &lt;RadzenMenuItem Text="Save" Value="save" Icon="save" /&gt;
    ///     &lt;/RadzenMenuItem&gt;
    /// &lt;/RadzenMenu&gt;
    /// </code>
    /// </example>
    public partial class RadzenMenu : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets whether the menu should automatically collapse to a hamburger menu on small screens.
        /// When enabled, displays a toggle button that expands/collapses the menu on mobile devices.
        /// </summary>
        /// <value><c>true</c> to enable responsive behavior with hamburger menu; <c>false</c> for always-horizontal menu. Default is <c>true</c>.</value>
        [Parameter]
        public bool Responsive { get; set; } = true;

        /// <summary>
        /// Gets or sets the interaction mode for opening submenus.
        /// When true, submenus open on click. When false, submenus open on hover (desktop) and click (touch devices).
        /// </summary>
        /// <value><c>true</c> to open on click; <c>false</c> to open on hover. Default is <c>true</c>.</value>
        [Parameter]
        public bool ClickToOpen { get; set; } = true;

        private bool IsOpen { get; set; } = false;

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-menu")
                                                                     .Add("rz-menu-open", Responsive && IsOpen)
                                                                     .Add("rz-menu-closed", Responsive && !IsOpen)
                                                                     .ToString();

        void OnToggle()
        {
            IsOpen = !IsOpen;
        }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }

        [Inject]
        NavigationManager NavigationManager { get; set; }

        bool subMenuOpen = false;
        internal int focusedIndex = -1;
        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (currentItems == null)
            {
                currentItems = items.Where(i => i.Visible && !i.Disabled).ToList();
            }

            if (key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;

                if (subMenuOpen)
                {
                    focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowUp" ? -1 : 1), 0, currentItems.Count - 1);
                }
                else
                {
                    if (key == "ArrowDown")
                    {
                        var item = currentItems[focusedIndex];

                        if (item.items.Any())
                        {
                            currentItems = item.items.Where(i => i.Visible && !i.Disabled).ToList();
                            focusedIndex = -1;
                            subMenuOpen = true;
                            await item.Open();
                        }
                    }
                }
            }
            else if (key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyPress = true;

                bool shouldOpenNextMenu = false;
                if (subMenuOpen)
                {
                    var parentItem = currentItems.FirstOrDefault().ParentItem;
                    if (parentItem != null)
                    {
                        currentItems = parentItem.Parent.items.Where(i => i.Visible && !i.Disabled).ToList();
                        focusedIndex = currentItems.IndexOf(parentItem);
                        subMenuOpen = false;
                        await parentItem.Close();
                        shouldOpenNextMenu = true;
                    }
                }

                focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowLeft" ? -1 : 1), 0, currentItems.Count - 1);

                if (shouldOpenNextMenu)
                {
                    shouldOpenNextMenu = false;

                    var item = currentItems[focusedIndex];

                    if (item.items.Any())
                    {
                        currentItems = item.items.Where(i => i.Visible && !i.Disabled).ToList();
                        focusedIndex = -1;
                        subMenuOpen = true;
                        await item.Toggle();
                    }
                }
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < currentItems.Count)
                {
                    var item = currentItems[focusedIndex];

                    if (item.items.Any())
                    {
                        currentItems = item.items.Where(i => i.Visible && !i.Disabled).ToList();
                        focusedIndex = -1;
                        subMenuOpen = true;
                        await item.Toggle();
                    }
                    else
                    {
                        if (item.Path != null)
                        {
                            NavigationManager.NavigateTo(item.Path);
                        }
                        else
                        {
                            await item.OnClick(new MouseEventArgs());
                        }
                    }
                }
            }
            else if (key == "Escape")
            {
                preventKeyPress = true;

                if (currentItems.Any(i => i.ParentItem != null))
                {
                    var parentItem = currentItems.FirstOrDefault().ParentItem;
                    if (parentItem != null)
                    {
                        currentItems = (parentItem.ParentItem != null ? parentItem.ParentItem.items : parentItem.Parent.items).Where(i => i.Visible && !i.Disabled).ToList();
                        focusedIndex = currentItems.IndexOf(parentItem);
                        subMenuOpen = false;
                        await parentItem.Close();
                    }
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }

        internal bool IsFocused(RadzenMenuItem item)
        {
            return currentItems?.IndexOf(item) == focusedIndex && focusedIndex != -1;
        }

        List<RadzenMenuItem> currentItems;

        internal List<RadzenMenuItem> items = new List<RadzenMenuItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenMenuItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Gets or sets the add button aria-label attribute.
        /// </summary>
        [Parameter]
        public string ToggleAriaLabel { get; set; } = "Toggle";

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        private void OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            IsOpen = false;
            StateHasChanged();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        void OnFocus()
        {
            focusedIndex = focusedIndex == -1 ? 0 : focusedIndex;
        }
    }
}