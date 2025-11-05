namespace Radzen;

/// <summary>
/// Specifies the time unit of <see cref="System.TimeSpan"/>.
/// </summary>
public enum TimeSpanUnit
{
    /// <summary>
    /// Day.
    /// </summary>
    Day = 0,

    /// <summary>
    /// Hour.
    /// </summary>
    Hour = 1,

    /// <summary>
    /// Minute.
    /// </summary>
    Minute = 2,

    /// <summary>
    /// Second.
    /// </summary>
    Second = 3,

    /// <summary>
    /// Millisecond.
    /// </summary>
    Millisecond = 4
#if NET7_0_OR_GREATER
    ,
    /// <summary>
    /// Microsecond.
    /// </summary>
    Microsecond = 5
#endif
}

