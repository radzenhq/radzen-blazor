# RadzenGoogleMap API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| ApiKey | `string?` | Gets or sets the Google API key. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Center | `GoogleMapPosition` | Gets or sets the center map position. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable<RadzenGoogleMapMarker>?` | Gets or sets the data - collection of RadzenGoogleMapMarker. |
| FitBoundsToMarkersOnUpdate | `bool` | Flag indicating whether map will be zoomed to marker bounds on update or not. |
| MapId | `string?` | Gets or sets the Google Map Id. |
| Markers | `RenderFragment?` | Gets or sets the markers. |
| Options | `Dictionary<string, object>?` | Gets or sets the Google map options: https://developers.google.com/maps/documentation/javascript/reference/map#MapOptions. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| Zoom | `double` | Gets or sets the zoom. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MapClick | `EventCallback<GoogleMapClickEventArgs>` | Gets or sets the map click callback. |
| MarkerClick | `EventCallback<RadzenGoogleMapMarker>` | Gets or sets the marker click callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddMarker(RadzenGoogleMapMarker marker) | `void` | Adds the marker. |
| OnMapClick(GoogleMapClickEventArgs args) | `System.Threading.Tasks.Task` | Handles the MapClick event. |
| OnMarkerClick(RadzenGoogleMapMarker marker) | `System.Threading.Tasks.Task` | Called when marker click. |
| RemoveMarker(RadzenGoogleMapMarker marker) | `void` | Removes the marker. |

