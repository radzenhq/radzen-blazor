namespace Radzen.Documents;


/// <summary>
/// The shape drawn at the ends of an open stroked subpath.
/// </summary>
public enum LineCap
{
    /// <summary>The stroke is squared off at the endpoint (value 0).</summary>
    Butt = 0,

    /// <summary>A semicircular arc caps the endpoint (value 1).</summary>
    Round = 1,

    /// <summary>The stroke extends half its width past the endpoint, squared off (value 2).</summary>
    Square = 2,
}
