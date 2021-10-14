using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPanelMenu component.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    public partial class RadzenPanelMenu : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }

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

        /// <summary>
        /// Called when initialized.
        /// </summary>
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

        /// <summary>
        /// Disposes this instance.
        /// </summary>
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

            return string.Equals(currentAbsoluteUrl, absoluteUrl, StringComparison.OrdinalIgnoreCase);
        }

        void SelectItem(RadzenPanelMenuItem item)
        {
            var selected = ShouldMatch(item.Path);
            item.Select(selected);
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-panel-menu";
        }
    }
}