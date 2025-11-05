namespace Radzen;

/// <summary>
/// Specifies the comparison operator of a filter.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Satisfied if the current value equals the specified value.
    /// </summary>
    Equals,

    /// <summary>
    /// Satisfied if the current value does not equal the specified value.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Satisfied if the current value is less than the specified value.
    /// </summary>
    LessThan,

    /// <summary>
    /// Satisfied if the current value is less than or equal to the specified value.
    /// </summary>
    LessThanOrEquals,

    /// <summary>
    /// Satisfied if the current value is greater than the specified value.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Satisfied if the current value is greater than or equal to the specified value.
    /// </summary>
    GreaterThanOrEquals,

    /// <summary>
    /// Satisfied if the current value contains the specified value.
    /// </summary>
    Contains,

    /// <summary>
    /// Satisfied if the current value starts with the specified value.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Satisfied if the current value ends with the specified value.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Satisfied if the current value does not contain the specified value.
    /// </summary>
    DoesNotContain,

    /// <summary>
    /// Satisfied if the current value is in the specified value.
    /// </summary>
    In,

    /// <summary>
    /// Satisfied if the current value is not in the specified value.
    /// </summary>
    NotIn,

    /// <summary>
    /// Satisfied if the current value is null.
    /// </summary>
    IsNull,

    /// <summary>
    /// Satisfied if the current value is <see cref="string.Empty"/>.
    /// </summary>
    IsEmpty,

    /// <summary>
    /// Satisfied if the current value is not null.
    /// </summary>
    IsNotNull,

    /// <summary>
    /// Satisfied if the current value is not <see cref="string.Empty"/>.
    /// </summary>
    IsNotEmpty,

    /// <summary>
    /// Custom operator if not need to generate the filter.
    /// </summary>
    Custom
}

