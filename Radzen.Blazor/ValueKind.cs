namespace Radzen;

/// <summary>
/// Represents the kind of value stored in a token.
/// </summary>
internal enum ValueKind
{
    /// <summary>
    /// No value.
    /// </summary>
    None,

    /// <summary>
    /// String value.
    /// </summary>
    String,

    /// <summary>
    /// Integer value.
    /// </summary>
    Int,

    /// <summary>
    /// Float value.
    /// </summary>
    Float,

    /// <summary>
    /// Double value.
    /// </summary>
    Double,

    /// <summary>
    /// Decimal value.
    /// </summary>
    Decimal,

    /// <summary>
    /// Character value.
    /// </summary>
    Character,

    /// <summary>
    /// Null value.
    /// </summary>
    Null,

    /// <summary>
    /// True value.
    /// </summary>
    True,

    /// <summary>
    /// False value.
    /// </summary>
    False,

    /// <summary>
    /// Long value.
    /// </summary>
    Long,

    /// <summary>
    /// Unsigned integer value.
    /// </summary>
    UInt,

    /// <summary>
    /// Unsigned long value.
    /// </summary>
    ULong,
}

