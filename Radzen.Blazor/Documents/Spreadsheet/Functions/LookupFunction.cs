#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

class LookupFunction : FormulaFunction
{
    public override string Name => "LOOKUP";

    public override FunctionParameter[] Parameters =>
    [
        new("lookup_value", ParameterType.Single, isRequired: true),
        new("lookup_vector", ParameterType.Collection, isRequired: true) { IsReference = true },
        new("result_vector", ParameterType.Collection, isRequired: false) { IsReference = true }
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var lookup = arguments.GetSingle("lookup_value");

        if (lookup is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (lookup.IsError)
        {
            return lookup;
        }

        if (GetArea(arguments, "lookup_vector") is not { } searched)
        {
            return CellData.FromError(CellError.Value);
        }

        var result = GetArea(arguments, "result_vector");
        var horizontal = searched.Columns > searched.Rows;
        var count = horizontal ? searched.Columns : searched.Rows;
        var position = -1;

        for (var i = 0; i < count; i++)
        {
            var cell = horizontal ? searched.CellAt(0, i) : searched.CellAt(i, 0);

            if (!RangeSearch.IsSameKind(cell, lookup))
            {
                continue;
            }

            if (cell.CompareTo(lookup) > 0)
            {
                break;
            }

            position = i;
        }

        if (position < 0)
        {
            return CellData.FromError(CellError.NA);
        }

        if (result is null)
        {
            return horizontal ? searched.CellAt(searched.Rows - 1, position) : searched.CellAt(position, searched.Columns - 1);
        }

        if (!result.IsReference && position >= result.Rows * result.Columns)
        {
            return CellData.FromError(CellError.NA);
        }

        return result.Rows == 1 && result.Columns > 1 ? result.CellAt(0, position) : result.CellAt(position, 0);
    }

    private static Area? GetArea(FunctionArguments arguments, string parameterName)
    {
        if (arguments.GetReference(parameterName) is { } reference)
        {
            return new Area(reference.Rows, reference.Columns, true, (row, column) => reference[row, column]);
        }

        return arguments.GetRange(parameterName) switch
        {
            RangeList range => new Area(range.Rows, range.Columns, false, (row, column) => range[row * range.Columns + column]),
            List<CellData> list => new Area(list.Count, 1, false, (row, _) => list[row]),
            _ => null
        };
    }

    private sealed record Area(int Rows, int Columns, bool IsReference, Func<int, int, CellData> CellAt);
}
