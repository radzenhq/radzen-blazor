#nullable enable

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Defines the type of a function parameter.
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// A single value (number, string, logical, cell, etc.).
    /// </summary>
    Single,
    
    /// <summary>
    /// A collection of values (range or multiple arguments).
    /// </summary>
    Collection,
    
    /// <summary>
    /// Multiple single values (repeating single parameters).
    /// </summary>
    Sequence
}

/// <summary>
/// Defines a parameter for a formula function.
/// </summary>
/// <remarks>
/// Initializes a new instance of the FunctionParameter class.
/// </remarks>
/// <param name="name">The name of the parameter.</param>
/// <param name="type">The type of the parameter.</param>
/// <param name="isRequired">Whether this parameter is required.</param>
public class FunctionParameter(string name, ParameterType type, bool isRequired = true)
{
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the type of the parameter.
    /// </summary>
    public ParameterType Type { get; } = type;

    /// <summary>
    /// Gets a value indicating whether this parameter is required.
    /// </summary>
    public bool IsRequired { get; } = isRequired;
}