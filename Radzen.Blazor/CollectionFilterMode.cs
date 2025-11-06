namespace Radzen;

/// <summary>
/// Specifies how the filter should be applied to a collection of items.
/// </summary>
public enum CollectionFilterMode
{
    /// <summary>
    /// The filter condition is satisfied if at least one item in the collection matches.
    /// </summary>
    Any,

    /// <summary>
    /// The filter condition is satisfied only if all items in the collection match.
    /// </summary>
    All
}

