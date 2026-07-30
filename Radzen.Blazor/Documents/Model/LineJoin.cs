namespace Radzen.Documents;


/// <summary>
/// The shape drawn at the corners where stroked subpath segments meet.
/// </summary>
public enum LineJoin
{
    /// <summary>The outer edges extend to meet at a sharp point.</summary>
    Miter = 0,

    /// <summary>A circular arc rounds the corner.</summary>
    Round = 1,

    /// <summary>The corner is squared off between the segment ends.</summary>
    Bevel = 2,
}
