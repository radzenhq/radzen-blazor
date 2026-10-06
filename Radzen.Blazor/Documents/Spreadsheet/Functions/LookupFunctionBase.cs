#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

abstract class LookupFunctionBase : FormulaFunction
{
    public override FunctionParameter[] Parameters =>
    [
        new("search_key", ParameterType.Single, isRequired: true),
        new("range", ParameterType.Collection, isRequired: true) { IsReference = true },
        new("index", ParameterType.Single, isRequired: true),
        new("is_sorted", ParameterType.Single, isRequired: false)
    ];

    protected abstract (int searchCount, int resultCount) GetSearchAndResultCounts(int rows, int columns);

    protected abstract (int rows, int columns) GetFallbackDimensions(int count);

    protected abstract int GetSearchCellIndex(int i, int columns);

    protected abstract int GetResultCellIndex(int matchPosition, int requestedIndex, int columns);

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var searchKey = arguments.GetSingle("search_key");
        var reference = arguments.GetReference("range");
        var rangeArg = reference is null ? arguments.GetRange("range") : null;
        var indexArg = arguments.GetSingle("index");
        var isSortedArg = arguments.GetSingle("is_sorted");

        if (searchKey is null || (reference is null && rangeArg is null) || indexArg is null)
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
        if (reference is not null)
        {
            rows = reference.Rows;
            columns = reference.Columns;
        }
        else if (rangeArg is RangeList rl)
        {
            rows = rl.Rows;
            columns = rl.Columns;
        }
        else
        {
            (rows, columns) = GetFallbackDimensions(rangeArg!.Count);
        }

        if (rows <= 0 || columns <= 0)
        {
            return CellData.FromError(CellError.Value);
        }

        Func<int, CellData> cellAt = reference is not null ? index => reference[index] : index => rangeArg![index];

        var (searchCount, resultCount) = GetSearchAndResultCounts(rows, columns);

        var index = indexArg.GetValueOrDefault<int?>();
        if (index is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (index.Value < 1 || index.Value > resultCount)
        {
            return CellData.FromError(CellError.Ref);
        }

        var isSorted = false;
        if (isSortedArg is not null && !isSortedArg.IsEmpty)
        {
            var maybeBool = isSortedArg.GetValueOrDefault<bool?>();
            if (maybeBool is null)
            {
                return CellData.FromError(CellError.Value);
            }
            isSorted = maybeBool.Value;
        }

        int matchPosition = -1;

        if (!isSorted)
        {
            for (int i = 0; i < searchCount; i++)
            {
                var cell = cellAt(GetSearchCellIndex(i, columns));
                if (cell.IsError)
                {
                    return cell;
                }
                if (cell.IsEqualTo(searchKey))
                {
                    matchPosition = i;
                    break;
                }
            }

            if (matchPosition == -1)
            {
                return CellData.FromError(CellError.NA);
            }
        }
        else
        {
            int lastCandidate = -1;

            for (int i = 0; i < searchCount; i++)
            {
                var cell = cellAt(GetSearchCellIndex(i, columns));
                if (cell.IsError)
                {
                    return cell;
                }

                if (cell.IsLessThanOrEqualTo(searchKey))
                {
                    lastCandidate = i;
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

            matchPosition = lastCandidate;
        }

        return cellAt(GetResultCellIndex(matchPosition, index.Value, columns));
    }
}
