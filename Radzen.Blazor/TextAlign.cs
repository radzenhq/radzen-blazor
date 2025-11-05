namespace Radzen;

/// <summary>
/// Specifies text alignment. Usually rendered as CSS <c>text-align</c> attribute.
/// </summary>
public enum TextAlign
{
    /// <summary>
    /// The text is aligned to the left side of its container.
    /// </summary>
    Left,

    /// <summary>
    /// The text is aligned to the right side of its container.
    /// </summary>
    Right,

    /// <summary>
    /// The text is centered in its container.
    /// </summary>
    Center,

    /// <summary>
    /// The text is justified.
    /// </summary>
    Justify,

    /// <summary>
    /// Same as justify, but also forces the last line to be justified.
    /// </summary>
    JustifyAll,

    /// <summary>
    /// The same as left if direction is left-to-right and right if direction is right-to-left.
    /// </summary>
    Start,

    /// <summary>
    /// The same as right if direction is left-to-right and left if direction is right-to-left.
    /// </summary>
    End
}

