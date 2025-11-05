namespace Radzen;

/// <summary>
/// Specifies the filter case sensitivity of a component.
/// </summary>
public enum FilterCaseSensitivity
{
    /// <summary>
    /// Relies on the underlying provider (LINQ to Objects, Entity Framework etc.) to handle case sensitivity. LINQ to Objects is case sensitive. Entity Framework relies on the database collection settings.
    /// </summary>
    Default,

    /// <summary>
    /// Filters are case insensitive regardless of the underlying provider.
    /// </summary>
    CaseInsensitive
}

