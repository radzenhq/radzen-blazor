using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPanelMenu component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenPanelMenu&gt;
    ///     &lt;RadzenPanelMenuItem Text="Data"&gt;
    ///         &lt;RadzenPanelMenuItem Text="Orders" Path="orders" /&gt;
    ///         &lt;RadzenPanelMenuItem Text="Employees" Path="employees" /&gt;
    ///     &lt;/RadzenPanelMenuItemItem&gt;
    /// &lt;/RadzenPanelMenu&gt;
    /// </code>
    /// </example>
    public partial class RadzenPanelMenu : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets a value indicating whether multiple items can be expanded.
        /// </summary>
        /// <value><c>true</c> if multiple items can be expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; } = true;

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }

        /// <summary>
        /// Gets or sets a value representing the URL matching behavior.
        /// </summary>
        [Parameter]
        public NavLinkMatch Match { get; set; }

        /// <summary>
        /// Gets or sets the display style.
        /// </summary>
        [Parameter]
        public MenuItemDisplayStyle DisplayStyle { get; set; } = MenuItemDisplayStyle.IconAndText;

        /// <summary>
        /// Gets or sets the show arrow.
        /// </summary>
        [Parameter]
        public bool ShowArrow { get; set; } = true;

        internal List<RadzenPanelMenuItem> items = new List<RadzenPanelMenuItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenPanelMenuItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
            }
        }

        internal async Task CollapseAllAsync(IEnumerable<RadzenPanelMenuItem> itemsToSkip)
        {
            var itemsToCollapse = items.Concat(items.SelectManyRecursive(i => i.items)).Except(itemsToSkip);

            foreach (var item in itemsToCollapse)
            {
                await item.CollapseAsync();
            }

            StateHasChanged();
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Multiple), Multiple))
            {
                await CollapseAllAsync([]);
            }

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-panel-menu";
        }

        [Inject]
        NavigationManager NavigationManager { get; set; }

        internal int focusedIndex = -1;
        List<RadzenPanelMenuItem> currentItems;

        bool preventKeyPress = false;

        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;

            currentItems ??= [.. items.Where(i => i.Visible)];

            if (key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;

                if (key == "ArrowUp" && focusedIndex == 0 && currentItems.Any(i => i.ParentItem != null))
                {
                    var parentItem = currentItems.FirstOrDefault().ParentItem;
                    currentItems = (parentItem.ParentItem != null ? parentItem.ParentItem.items : parentItem.Parent.items).ToList();
                    focusedIndex = currentItems.IndexOf(parentItem);
                }
                else if (key == "ArrowDown" && currentItems.ElementAtOrDefault(focusedIndex) != null &&
                    currentItems.ElementAtOrDefault(focusedIndex).IsExpanded && currentItems.ElementAtOrDefault(focusedIndex).items.Any())
                {
                    currentItems = currentItems.ElementAtOrDefault(focusedIndex).items.Where(i => i.Visible).ToList();
                    focusedIndex = 0;
                }
                else if (key == "ArrowDown" && focusedIndex == currentItems.Count - 1)
                {
                    var parentItem = currentItems.FirstOrDefault().ParentItem;
                    currentItems = (parentItem?.ParentItem != null ? parentItem.ParentItem.items : parentItem != null ? parentItem.Parent.items : items).Where(i => i.Visible).ToList();
                    focusedIndex = parentItem != null ? currentItems.IndexOf(parentItem) + 1 : focusedIndex;
                }
                else if (key == "ArrowUp" && currentItems.ElementAtOrDefault(focusedIndex - 1) != null &&
                    currentItems.ElementAtOrDefault(focusedIndex - 1).IsExpanded && currentItems.ElementAtOrDefault(focusedIndex - 1).items.Any())
                {
                    currentItems = currentItems.ElementAtOrDefault(focusedIndex - 1).items.Where(i => i.Visible).ToList();
                    focusedIndex = currentItems.Count - 1;
                }
                else
                {
                    focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowUp" ? -1 : 1), 0, currentItems.Count - 1);
                }

                try
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.scrollIntoViewIfNeeded", currentItems[focusedIndex].Element);
                }
                catch
                { }
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < currentItems.Count)
                {
                    var item = currentItems[focusedIndex];

                    if (item.items.Count > 0)
                    {
                        await item.Toggle();

                        currentItems = (item.IsExpanded ?
                                item.items :
                                item.ParentItem != null ? item.ParentItem.items : item.Parent.items).Where(i => i.Visible).ToList();

                        focusedIndex = item.IsExpanded ? 0 : currentItems.IndexOf(item);
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
            else
            {
                preventKeyPress = false;
            }

            if (preventKeyPress)
            {
                StateHasChanged();
            }
        }

        internal bool IsFocused(RadzenPanelMenuItem item)
        {
            return currentItems?.IndexOf(item) == focusedIndex && focusedIndex != -1;
        }

        internal void RemoveItem(RadzenPanelMenuItem item)
        {
            items.Remove(item);

            focusedIndex = -1;
            currentItems = null;
        }

        void OnFocus()
        {
            currentItems ??= [.. items.Where(i => i.Visible)];

            if (focusedIndex == -1)
            {
                focusedIndex = 0;

                StateHasChanged();
            }
        }
    }
}