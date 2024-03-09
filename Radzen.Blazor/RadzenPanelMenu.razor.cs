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
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                SelectItem(item);
                StateHasChanged();
            }
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            UriHelper.LocationChanged += UriHelper_OnLocationChanged;
        }

        private void UriHelper_OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            var allExpandedItems = items.Concat(items.SelectManyRecursive(i => i.ExpandedInternal ? i.items : Enumerable.Empty<RadzenPanelMenuItem>()));
            foreach (var item in allExpandedItems)
            {
                SelectItem(item);
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            UriHelper.LocationChanged -= UriHelper_OnLocationChanged;
        }

        internal void CollapseAll(IEnumerable<RadzenPanelMenuItem> itemsToSkip)
        {
            items.Concat(items.SelectManyRecursive(i => i.items))
                .Where(i => !itemsToSkip.Contains(i)).ToList().ForEach(i => InvokeAsync(i.Collapse));
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Multiple), Multiple))
            {
                CollapseAll(Enumerable.Empty<RadzenPanelMenuItem>());
            }

            await base.SetParametersAsync(parameters);
        }

        bool ShouldMatch(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            var currentAbsoluteUrl = UriHelper.ToAbsoluteUri(UriHelper.Uri).AbsoluteUri;
            var absoluteUrl = UriHelper.ToAbsoluteUri(url).AbsoluteUri;

            if (EqualsHrefExactlyOrIfTrailingSlashAdded(absoluteUrl, currentAbsoluteUrl))
            {
                return true;
            }

            if (url == "/")
            {
                return false;
            }

            if (Match == NavLinkMatch.Prefix
                && IsStrictlyPrefixWithSeparator(currentAbsoluteUrl, absoluteUrl))
            {
                return true;
            }

            return false;
        }

        private static bool EqualsHrefExactlyOrIfTrailingSlashAdded(string absoluteUrl, string currentAbsoluteUrl)
        {
            if (string.Equals(currentAbsoluteUrl, absoluteUrl, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (currentAbsoluteUrl.Length == absoluteUrl.Length - 1)
            {
                if (absoluteUrl[absoluteUrl.Length - 1] == '/'
                    && absoluteUrl.StartsWith(currentAbsoluteUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSeparator(char c)
        {
            return c == '?' || c == '/';
        }

        private static bool IsStrictlyPrefixWithSeparator(string value, string prefix)
        {
            var prefixLength = prefix.Length;
            if (value.Length > prefixLength)
            {
                return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && (
                        prefixLength == 0
                        || IsSeparator(prefix[prefixLength - 1])
                        || IsSeparator(value[prefixLength])
                    );
            }
            else
            {
                return false;
            }
        }

        internal void SelectItem(RadzenPanelMenuItem item)
        {
            var selected = ShouldMatch(item.Path);
            item.Select(selected);
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
            var key = args.Code != null ? args.Code : args.Key;

            if (currentItems == null)
            {
                currentItems = items.Where(i => i.Visible).ToList();
            }

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
                    currentItems.ElementAtOrDefault(focusedIndex).ExpandedInternal && currentItems.ElementAtOrDefault(focusedIndex).items.Any())
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
                    currentItems.ElementAtOrDefault(focusedIndex - 1).ExpandedInternal && currentItems.ElementAtOrDefault(focusedIndex - 1).items.Any())
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

                    if (item.items.Any())
                    {
                        await item.Toggle();

                        currentItems = (item.ExpandedInternal ?
                                item.items :
                                item.ParentItem != null ? item.ParentItem.items : item.Parent.items).Where(i => i.Visible).ToList();

                        focusedIndex = item.ExpandedInternal ? 0 : currentItems.IndexOf(item);
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
    }
}
