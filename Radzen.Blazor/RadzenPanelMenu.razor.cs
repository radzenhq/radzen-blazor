using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;

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

        List<RadzenPanelMenuItem> items = new List<RadzenPanelMenuItem>();

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
            foreach (var item in items)
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

        void SelectItem(RadzenPanelMenuItem item)
        {
            var selected = ShouldMatch(item.Path);
            item.Select(selected);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-panel-menu";
        }
    }
}
