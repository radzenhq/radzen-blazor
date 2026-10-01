namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenGoogleMap.BoundsChanged" /> event that is being raised.
/// </summary>
public class GoogleMapBoundsChangedEventArgs
{
    /// <summary>
    /// The north-east corner of the visible map area.
    /// </summary>
    public GoogleMapPosition? NorthEast { get; set; }

    /// <summary>
    /// The south-west corner of the visible map area.
    /// </summary>
    public GoogleMapPosition? SouthWest { get; set; }

    /// <summary>
    /// The center of the visible map area.
    /// </summary>
    public GoogleMapPosition? Center { get; set; }

    /// <summary>
    /// The current zoom level of the map.
    /// </summary>
    public double Zoom { get; set; }
}
