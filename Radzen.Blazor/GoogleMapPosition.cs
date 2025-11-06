using System;

namespace Radzen;

/// <summary>
/// A class that represents a <see cref="Radzen.Blazor.RadzenGoogleMap" /> position.
/// </summary>
public class GoogleMapPosition : IEquatable<GoogleMapPosition>
{
    /// <summary>
    /// Gets or sets the latitude.
    /// </summary>
    /// <value>The latitude.</value>
    public double Lat { get; set; }

    /// <summary>
    /// Gets or sets the longitude.
    /// </summary>
    /// <value>The longitude.</value>
    public double Lng { get; set; }

    /// <inheritdoc />
    public bool Equals(GoogleMapPosition other)
    {
        if (other != null)
        {
            return this.Lat == other.Lat && this.Lng == other.Lng;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return this.Equals(obj as GoogleMapPosition);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

