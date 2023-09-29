using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenGoogleMap component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenGoogleMap Zoom="3" Center=@(new GoogleMapPosition() { Lat = 42.6977, Lng = 23.3219 }) MapClick=@OnMapClick MarkerClick=@OnMarkerClick"&gt;
    ///     &lt;Markers&gt;
    ///         &lt;RadzenGoogleMapMarker Title="London" Label="London" Position=@(new GoogleMapPosition() { Lat = 51.5074, Lng = 0.1278 }) /&gt;
    ///         &lt;RadzenGoogleMapMarker Title="Paris " Label="Paris" Position=@(new GoogleMapPosition() { Lat = 48.8566, Lng = 2.3522 }) /&gt;
    ///     &lt;/Markers&gt;
    /// &lt;/RadzenGoogleMap&gt;
    /// @code {
    ///   void OnMapClick(GoogleMapClickEventArgs args)
    ///   {
    ///     Console.WriteLine($"Map clicked at Lat: {args.Position.Lat}, Lng: {args.Position.Lng}");
    ///   }
    ///   
    ///   void OnMarkerClick(RadzenGoogleMapMarker marker)
    ///   {
    ///     Console.WriteLine($"Map {marker.Title} marker clicked. Marker position -> Lat: {marker.Position.Lat}, Lng: {marker.Position.Lng}");
    ///   }
    /// }
    /// </code>
    /// </example>
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
        /// Gets or sets the Google map options: https://developers.google.com/maps/documentation/javascript/reference/map#MapOptions.
        /// </summary>
        /// <value>The Google map options.</value>
        [Parameter]
        public Dictionary<string, object> Options { get; set; }

        double zoom = 8;
        /// <summary>
        /// Gets or sets the zoom.
        /// </summary>
        /// <value>The zoom.</value>
        [Parameter]
        public double Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                if (zoom != value)
                {
                    zoom = value;

                    InvokeAsync(UpdateMap);
                }
            }
        }

        /// <summary>
        /// Flag indicating whether map will be zoomed to marker bounds on update or not.
        /// </summary>
        [Parameter]
        public bool FitBoundsToMarkersOnUpdate { get; set; } = false;

        GoogleMapPosition center = new GoogleMapPosition() { Lat = 0, Lng = 0 };
        /// <summary>
        /// Gets or sets the center map position.
        /// </summary>
        /// <value>The center.</value>
        [Parameter]
        public GoogleMapPosition Center
        {
            get
            {
                return center;
            }
            set
            {
                if (!object.Equals(center, value))
                {
                    center = value;

                    InvokeAsync(UpdateMap);
                }
            }
        }

        async Task UpdateMap()
        {
            if (!firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.updateMap", UniqueID, Zoom, Center);
            }
        }

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

        /// <inheritdoc />
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

        bool firstRender = true;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            var data = Data != null ? Data : markers;

            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.createMap", Element, Reference, UniqueID, ApiKey, Zoom, Center,
                     data.Select(m => new { Title = m.Title, Label = m.Label, Position = m.Position }), Options, FitBoundsToMarkersOnUpdate);
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("Radzen.updateMap", UniqueID, null, null,
                             data.Select(m => new { Title = m.Title, Label = m.Label, Position = m.Position }), Options, FitBoundsToMarkersOnUpdate);
            }
        }

        /// <inheritdoc />
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
