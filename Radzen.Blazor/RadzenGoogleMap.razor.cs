using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenGoogleMap component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenGoogleMap : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the data - collection of RadzenGoogleMapMarker.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable<RadzenGoogleMapMarker> Data { get; set; }

        /// <summary>
        /// Gets or sets the map click callback.
        /// </summary>
        /// <value>The map click callback.</value>
        [Parameter]
        public EventCallback<GoogleMapClickEventArgs> MapClick { get; set; }

        /// <summary>
        /// Gets or sets the marker click callback.
        /// </summary>
        /// <value>The marker click callback.</value>
        [Parameter]
        public EventCallback<RadzenGoogleMapMarker> MarkerClick { get; set; }

        /// <summary>
        /// Gets or sets the Google API key.
        /// </summary>
        /// <value>The Google API key.</value>
        [Parameter]
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the zoom.
        /// </summary>
        /// <value>The zoom.</value>
        [Parameter]
        public double Zoom { get; set; } = 8;

        /// <summary>
        /// Gets or sets the center map position.
        /// </summary>
        /// <value>The center.</value>
        [Parameter]
        public GoogleMapPosition Center { get; set; } = new GoogleMapPosition() { Lat = 0, Lng = 0 };

        /// <summary>
        /// Gets or sets the markers.
        /// </summary>
        /// <value>The markers.</value>
        [Parameter]
        public RenderFragment Markers { get; set; }

        List<RadzenGoogleMapMarker> markers = new List<RadzenGoogleMapMarker>();

        /// <summary>
        /// Adds the marker.
        /// </summary>
        /// <param name="marker">The marker.</param>
        public void AddMarker(RadzenGoogleMapMarker marker)
        {
            if (markers.IndexOf(marker) == -1)
            {
                markers.Add(marker);
            }
        }

        /// <summary>
        /// Removes the marker.
        /// </summary>
        /// <param name="marker">The marker.</param>
        public void RemoveMarker(RadzenGoogleMapMarker marker)
        {
            if (markers.IndexOf(marker) != -1)
            {
                markers.Remove(marker);
            }
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-map";
        }

        /// <summary>
        /// Handles the <see cref="E:MapClick" /> event.
        /// </summary>
        /// <param name="args">The <see cref="GoogleMapClickEventArgs"/> instance containing the event data.</param>
        [JSInvokable("RadzenGoogleMap.OnMapClick")]
        public async System.Threading.Tasks.Task OnMapClick(GoogleMapClickEventArgs args)
        {
            await MapClick.InvokeAsync(args);
        }

        /// <summary>
        /// Called when marker click.
        /// </summary>
        /// <param name="marker">The marker.</param>
        [JSInvokable("RadzenGoogleMap.OnMarkerClick")]
        public async System.Threading.Tasks.Task OnMarkerClick(RadzenGoogleMapMarker marker)
        {
            await MarkerClick.InvokeAsync(marker);
        }

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            var data = Data != null ? Data : markers;

            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.createMap", Element, Reference, UniqueID, ApiKey, Zoom, Center,
                     data.Select(m => new { Title = m.Title, Label = m.Label, Position = m.Position }));
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("Radzen.updateMap", UniqueID, Zoom, Center,
                             data.Select(m => new { Title = m.Title, Label = m.Label, Position = m.Position }));
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyMap", UniqueID);
            }
        }
    }
}