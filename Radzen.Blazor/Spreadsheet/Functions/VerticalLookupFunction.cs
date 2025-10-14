#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

class VerticalLookupFunction : FormulaFunction
{
    public override string Name => "VLOOKUP";

    public override FunctionParameter[] Parameters =>
    [
        new("search_key", ParameterType.Single, isRequired: true),
        new("range", ParameterType.Collection, isRequired: true),
        new("index", ParameterType.Single, isRequired: true),
        new("is_sorted", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var searchKey = arguments.GetSingle("search_key");
        var rangeArg = arguments.GetRange("range");
        var indexArg = arguments.GetSingle("index");
        var isSortedArg = arguments.GetSingle("is_sorted");

        if (searchKey == null || rangeArg == null || indexArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (searchKey.IsError)
        {
            return searchKey;
        }
        if (indexArg.IsError)
        {
            return indexArg;
        }

        // Determine rows/columns of range
        int rows;
        int columns;
        if (rangeArg is RangeList rl)
        {
            rows = rl.Rows;
            columns = rl.Columns;
        }
        else
        {
            // Fallback: assume single column range
            rows = rangeArg.Count;
            columns = 1;
        }

        if (rows <= 0 || columns <= 0)
        {
            return CellData.FromError(CellError.Value);
        }

        // Index is 1-based per VLOOKUP spec
        var index = indexArg.GetValueOrDefault<int?>();
        if (index is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var colIndex = index.Value;
        if (colIndex < 1 || colIndex > columns)
        {
            return CellData.FromError(CellError.Ref);
        }

        // Determine match mode
        var isSorted = false; // default to exact match when omitted (Excel/Google Sheets default false)
        if (isSortedArg != null && !isSortedArg.IsEmpty)
        {
            var maybeBool = isSortedArg.GetValueOrDefault<bool?>();
            if (maybeBool is null)
            {
                return CellData.FromError(CellError.Value);
            }
            isSorted = maybeBool.Value;
        }

        // Iterate rows; range is row-major in evaluator
        int matchRow = -1;

        if (!isSorted)
        {
            // Exact match: find first row whose first column equals searchKey
            for (int r = 0; r < rows; r++)
            {
                var firstCell = rangeArg[r * columns + 0];
                if (firstCell.IsError)
                {
                    return firstCell;
                }
                if (firstCell.IsEqualTo(searchKey))
                {
                    matchRow = r;
                    break;
                }
            }

            if (matchRow == -1)
            {
                return CellData.FromError(CellError.NA);
            }
        }
        else
        {
            // Approximate match: last value <= searchKey in first column
            // Assumes first column sorted ascending
            int lastCandidate = -1;

            for (int r = 0; r < rows; r++)
            {
                var firstCell = rangeArg[r * columns + 0];
                if (firstCell.IsError)
                {
                    return firstCell;
                }

                // If firstCell <= searchKey, update candidate
                if (firstCell.IsLessThanOrEqualTo(searchKey))
                {
                    lastCandidate = r;
                }
                else
                {
                    // Since sorted ascending, further rows will be greater
                    break;
                }
            }

            if (lastCandidate == -1)
            {
                return CellData.FromError(CellError.NA);
            }

            matchRow = lastCandidate;
        }

        // Return value from matched row and requested column
        var resultCell = rangeArg[matchRow * columns + (colIndex - 1)];
        return resultCell;
    }
}


