namespace Radzen.Documents;


/// <summary>
/// The shape drawn at the corners where stroked subpath segments meet.
/// </summary>
public enum LineJoin
{
    /// <summary>The outer edges extend to meet at a sharp point (value 0).</summary>
    Miter = 0,

    /// <summary>A circular arc rounds the corner (value 1).</summary>
    Round = 1,

    /// <summary>The corner is squared off between the segment ends (value 2).</summary>
    Bevel = 2,
}
