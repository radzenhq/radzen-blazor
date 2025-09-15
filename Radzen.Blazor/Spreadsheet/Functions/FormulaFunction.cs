#nullable enable
using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Base class for formula functions that provides common functionality for evaluation.
/// </summary>
public abstract class FormulaFunction
{
    /// <summary>
    /// The current error state of the function evaluation.
    /// </summary>
    protected CellError? error;

    /// <summary>
    /// Gets the current error state.
    /// </summary>
    public CellError? Error => error;

    /// <summary>
    /// Gets a value indicating whether this function can handle error arguments.
    /// Functions that return true will receive error values as arguments instead of having evaluation short-circuited.
    /// </summary>
    public virtual bool CanHandleErrors => false;

    /// <summary>
    /// Evaluates the function with the given arguments.
    /// </summary>
    /// <param name="arguments">The function arguments as values.</param>
    /// <returns>The result value.</returns>
    public abstract object? Evaluate(List<object?> arguments);

    /// <summary>
    /// Returns true if the provided value is null.
    /// </summary>
    protected static bool IsNull(object? value) => value is null;

    /// <summary>
    /// Tries to get a CellError from a value.
    /// </summary>
    protected static bool TryGetError(object? value, out CellError errorValue) => ValueHelpers.TryGetError(value, out errorValue);

    /// <summary>
    /// Returns true if the provided value is a numeric type supported by the evaluator.
    /// </summary>
    protected static bool IsNumeric(object? value) => ValueHelpers.IsNumeric(value);

    /// <summary>
    /// Converts a numeric value to double.
    /// </summary>
    protected static double ToDouble(object value) => ValueHelpers.ToDouble(value);

    /// <summary>
    /// Converts a value to a boolean using spreadsheet semantics.
    /// </summary>
    protected static bool ConvertToBoolean(object? value) => ValueHelpers.ConvertToBoolean(value);

    /// <summary>
    /// Converts a value to string using spreadsheet semantics.
    /// </summary>
    protected static string ConvertToString(object? value) => ValueHelpers.ConvertToString(value);
}