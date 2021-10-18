using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenPanelMenu.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    public partial class RadzenPanelMenu : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the click.
        /// </summary>
        /// <value>The click.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }

        /// <summary>
        /// The items
        /// </summary>
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
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            UriHelper.LocationChanged += UriHelper_OnLocationChanged;
        }

        /// <summary>
        /// Handles the OnLocationChanged event of the UriHelper control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Shoulds the match.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
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

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
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