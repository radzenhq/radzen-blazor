#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class HorizontalLookupFunction : FormulaFunction
{
    public override string Name => "HLOOKUP";

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

        int rows;
        int columns;
        if (rangeArg is RangeList rl)
        {
            rows = rl.Rows;
            columns = rl.Columns;
        }
        else
        {
            rows = 1;
            columns = rangeArg.Count;
        }

        if (rows <= 0 || columns <= 0)
        {
            return CellData.FromError(CellError.Value);
        }

        var index = indexArg.GetValueOrDefault<int?>();
        if (index is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var rowIndex = index.Value;
        if (rowIndex < 1 || rowIndex > rows)
        {
            return CellData.FromError(CellError.Ref);
        }

        var isSorted = false;
        if (isSortedArg != null && !isSortedArg.IsEmpty)
        {
            var maybeBool = isSortedArg.GetValueOrDefault<bool?>();
            if (maybeBool is null)
            {
                return CellData.FromError(CellError.Value);
            }
            isSorted = maybeBool.Value;
        }

        int matchColumn = -1;

        if (!isSorted)
        {
            for (int c = 0; c < columns; c++)
            {
                var topCell = rangeArg[0 * columns + c];
                if (topCell.IsError)
                {
                    return topCell;
                }
                if (topCell.IsEqualTo(searchKey))
                {
                    matchColumn = c;
                    break;
                }
            }

            if (matchColumn == -1)
            {
                return CellData.FromError(CellError.NA);
            }
        }
        else
        {
            int lastCandidate = -1;

            for (int c = 0; c < columns; c++)
            {
                var topCell = rangeArg[0 * columns + c];
                if (topCell.IsError)
                {
                    return topCell;
                }

                if (topCell.IsLessThanOrEqualTo(searchKey))
                {
                    lastCandidate = c;
                }
                else
                {
                    break;
                }
            }

            if (lastCandidate == -1)
            {
                return CellData.FromError(CellError.NA);
            }

            matchColumn = lastCandidate;
        }

        var resultCell = rangeArg[(rowIndex - 1) * columns + matchColumn];
        return resultCell;
    }
}