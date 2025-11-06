namespace Radzen;

/// <summary>
/// Represents whether items are forced onto one line or can wrap onto multiple lines.
/// </summary>
public enum FlexWrap
{
    /// <summary>
    /// The items are laid out in a single line.
    /// </summary>
    NoWrap,

    /// <summary>
    /// The items break into multiple lines.
    /// </summary>
    Wrap,

    /// <summary>
    /// The items break into multiple lines reversed.
    /// </summary>
    WrapReverse
}

