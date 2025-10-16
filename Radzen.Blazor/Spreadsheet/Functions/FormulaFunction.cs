#nullable enable
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Base class for formula functions that provides common functionality for evaluation.
/// </summary>
public abstract class FormulaFunction
{
    /// <summary>
    /// Gets a value indicating whether this function can handle error arguments.
    /// Functions that return true will receive error values as arguments instead of having evaluation short-circuited.
    /// </summary>
    public virtual bool CanHandleErrors => false;

    /// <summary>
    /// Gets the parameter definitions for this function.
    /// </summary>
    /// <returns>An array of parameter definitions describing the function's parameters.</returns>
    public abstract FunctionParameter[] Parameters { get; }

    /// <summary>
    /// Evaluates the function with the given arguments.
    /// </summary>
    /// <param name="arguments">The function arguments organized by parameter name.</param>
    /// <returns>The result value wrapped in CellData.</returns>
    public abstract CellData Evaluate(FunctionArguments arguments);

    /// <summary>
    /// Gets the name of the function.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Tries to get a string parameter from the function arguments.
    /// </summary>
    /// <param name="arguments">The function arguments organized by parameter name.</param>
    /// <param name="parameterName">The name of the parameter to get.</param>
    /// <param name="text">The string value of the parameter.</param>
    /// <param name="error">The error value if the parameter is not a string.</param>
    /// <returns>True if the parameter is a string, false otherwise.</returns>
    protected static bool TryGetString(FunctionArguments arguments, string parameterName, out string text, out CellData? error)
    {
        text = string.Empty;
        error = null;
        var arg = arguments.GetSingle(parameterName);

        if (arg == null)
        {
            error = CellData.FromError(CellError.Value);
            return false;
        }

        if (arg.IsError)
        {
            error = arg;
            return false;
        }

        text = arg.GetValueOrDefault<string?>() ?? string.Empty;
        return true;
    }

    /// <summary>
    /// Tries to get an integer parameter from the function arguments.
    /// </summary>
    /// <param name="arguments">The function arguments organized by parameter name.</param>
    /// <param name="parameterName">The name of the parameter to get.</param>
    /// <param name="isRequired">Whether the parameter is required.</param>
    /// <param name="defaultValue">The default value if the parameter is not provided.</param>
    /// <param name="value">The integer value of the parameter.</param>
    /// <param name="error">The error value if the parameter is not an integer.</param>
    protected static bool TryGetInteger(FunctionArguments arguments, string parameterName, bool isRequired, int? defaultValue, out int value, out CellData? error)
    {
        value = 0;
        error = null;
        var arg = arguments.GetSingle(parameterName);

        if (arg == null)
        {
            if (isRequired)
            {
                error = CellData.FromError(CellError.Value);
                return false;
            }
            value = defaultValue ?? 0;
            return true;
        }

        if (arg.IsError)
        {
            error = arg;
            return false;
        }

        if (!arg.TryGetInt(out value, allowBooleans: true, nonNumericTextAsZero: false))
        {
            error = CellData.FromError(CellError.Value);
            return false;
        }

        return true;
    }
}