# GoogleMap component
This article demonstrates how to use the GoogleMap component. 

```
<h3>Show marker for Madrid 
    <RadzenCheckBox @bind-Value=@showMadridMarker />
</h3>
<RadzenGoogleMap style="height: 400px" Zoom=@zoom Center=@(new GoogleMapPosition() { Lat = 42.6977, Lng = 23.3219 }) MapClick=@OnMapClick MarkerClick=@OnMarkerClick>
    <Markers>
        <RadzenGoogleMapMarker Title="London" Label="London" Position=@(new GoogleMapPosition() { Lat = 51.5074, Lng = 0.1278 }) />
        <RadzenGoogleMapMarker Title="Paris " Label="Paris" Position=@(new GoogleMapPosition() { Lat = 48.8566, Lng = 2.3522 }) />
        @if (showMadridMarker)
        { 
            <RadzenGoogleMapMarker Title="Madrid " Label="Madrid" Position=@(new GoogleMapPosition() { Lat = 40.4168, Lng = -3.7038 }) />
        }
    </Markers>
</RadzenGoogleMap>
@code {
    int zoom = 3;
    bool showMadridMarker;
    EventConsole console;

    void OnMapClick(GoogleMapClickEventArgs args)
    {
        console.Log($"Map clicked at Lat: {args.Position.Lat}, Lng: {args.Position.Lng}");
    }

    void OnMarkerClick(RadzenGoogleMapMarker marker)
    {
        console.Log($"Map {marker.Title} marker clicked. Marker position -> Lat: {marker.Position.Lat}, Lng: {marker.Position.Lng}");
    }
}
```
