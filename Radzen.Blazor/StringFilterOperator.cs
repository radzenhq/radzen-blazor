namespace Radzen;

/// <summary>
/// Specifies the string comparison operator of a filter.
/// </summary>
public enum StringFilterOperator
{
    /// <summary>
    /// Satisfied if a string contains the specified value.
    /// </summary>
    Contains,

    /// <summary>
    /// Satisfied if a string starts with the specified value.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Satisfied if a string ends with with the specified value.
    /// </summary>
    EndsWith
}

