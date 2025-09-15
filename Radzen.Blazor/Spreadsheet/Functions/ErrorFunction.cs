using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Implements an error function for handling missing or invalid formula functions.
/// </summary>
class ErrorFunction : FormulaFunction
{
    public override object? Evaluate(List<object?> arguments)
    {
        error = CellError.Name;
        return error;
    }
}
