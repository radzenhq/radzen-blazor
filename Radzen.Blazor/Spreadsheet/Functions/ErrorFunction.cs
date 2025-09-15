using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Implements an error function for handling missing or invalid formula functions.
/// </summary>
class ErrorFunction : FormulaFunction
{
    private readonly string functionName;

    /// <summary>
    /// Initializes a new instance of the ErrorFunction class.
    /// </summary>
    /// <param name="functionName">The name of the function that was not found.</param>
    public ErrorFunction(string functionName)
    {
        this.functionName = functionName;
    }

    /// <summary>
    /// Evaluates the error function by returning a #NAME? error.
    /// </summary>
    /// <param name="arguments">The function arguments (ignored).</param>
    /// <returns>An expression representing a #NAME? error.</returns>
    public override Expression Evaluate(List<Expression> arguments)
    {
        error = CellError.Name;
        return Expression.Constant(CellError.Name);
    }
}
