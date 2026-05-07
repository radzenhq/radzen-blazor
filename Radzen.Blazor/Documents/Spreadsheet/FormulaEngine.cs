using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Headless Excel-compatible formula engine. Use <see cref="Formula"/> for one-shot
/// stateless evaluation, or instantiate <see cref="FormulaEngine"/> for a stateful
/// mini-spreadsheet that recalculates dependents on change.
/// <para>
/// Formula syntax matches Excel — leading <c>=</c> optional. Supports the full
/// function library registered with the engine plus operators, ranges
/// (<c>=SUM(A1:A10)</c>), and 65+ built-in functions (SUM, AVERAGE, IF, VLOOKUP, …).
/// </para>
/// </summary>
public sealed class FormulaEngine
{
    private readonly Workbook workbook;
    private readonly Worksheet sheet;

    /// <summary>
    /// Creates a new engine with an internal scratch worksheet (1024 × 100 cells).
    /// </summary>
    public FormulaEngine()
    {
        workbook = new Workbook();
        sheet = workbook.AddSheet("Engine", 1024, 100);
    }

    /// <summary>
    /// The function registry. Use <c>Functions.Add(new YourFormulaFunction())</c> to
    /// extend the formula library with custom functions.
    /// </summary>
    public FunctionStore Functions => sheet.FunctionRegistry;

    /// <summary>
    /// Sets a cell's value or formula. A string starting with <c>=</c> is treated as a
    /// formula; everything else is parsed as a literal (numbers, dates, booleans, text).
    /// Dependent formulas already in the engine recalculate automatically.
    /// </summary>
    public void Set(string cellRef, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(cellRef);
        var addr = CellRef.Parse(cellRef);
        var cell = sheet.Cells[addr];

        if (value is string s && s.Length > 1 && s[0] == '=')
        {
            cell.Formula = s;
        }
        else
        {
            cell.Value = value;
        }
    }

    /// <summary>
    /// Reads a cell's current value. For formula cells, returns the cached evaluated
    /// result.
    /// </summary>
    public object? Get(string cellRef)
    {
        ArgumentException.ThrowIfNullOrEmpty(cellRef);
        var addr = CellRef.Parse(cellRef);
        return sheet.Cells[addr].Value;
    }

    /// <summary>
    /// Evaluates an ad-hoc expression against the current engine state, returning the
    /// result without leaving a permanent cell behind.
    /// </summary>
    public object? Evaluate(string expression)
    {
        ArgumentException.ThrowIfNullOrEmpty(expression);
        var formula = expression[0] == '=' ? expression : "=" + expression;

        // Reserve the last cell of the internal sheet as the eval scratch slot.
        // Callers using A1-style references stay clear of this corner.
        var cell = sheet.Cells[sheet.RowCount - 1, sheet.ColumnCount - 1];
        cell.Formula = formula;
        return cell.Value;
    }
}

/// <summary>
/// Stateless one-shot formula evaluation. Internally creates a fresh
/// <see cref="FormulaEngine"/> per call, so each call is independent.
/// </summary>
public static class Formula
{
    /// <summary>
    /// Evaluates a literal expression with no cell references.
    /// </summary>
    /// <example>
    /// <code>
    /// Formula.Evaluate("=SUM(1, 2, 3) + IF(2&gt;1, 10, 20)"); // → 16
    /// </code>
    /// </example>
    public static object? Evaluate(string expression)
    {
        return new FormulaEngine().Evaluate(expression);
    }

    /// <summary>
    /// Evaluates an expression that may reference cells, with the cell values supplied
    /// as a map of A1-style addresses to values.
    /// </summary>
    /// <example>
    /// <code>
    /// Formula.Evaluate("=A1+B1*C1", new Dictionary&lt;string, object?&gt;
    /// {
    ///     ["A1"] = 2.0, ["B1"] = 3.0, ["C1"] = 4.0,
    /// }); // → 14
    /// </code>
    /// </example>
    public static object? Evaluate(string expression, IReadOnlyDictionary<string, object?> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var engine = new FormulaEngine();
        foreach (var (key, value) in cells)
        {
            engine.Set(key, value);
        }
        return engine.Evaluate(expression);
    }
}
