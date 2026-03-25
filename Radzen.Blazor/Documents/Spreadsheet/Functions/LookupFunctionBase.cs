#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Shared implementation for VLOOKUP and HLOOKUP.
/// Subclasses provide the axis-specific details via a few one-liner overrides.
/// </summary>
abstract class LookupFunctionBase : FormulaFunction
{
    public override FunctionParameter[] Parameters =>
    [
        new("search_key", ParameterType.Single, isRequired: true),
        new("range", ParameterType.Collection, isRequired: true),
        new("index", ParameterType.Single, isRequired: true),
        new("is_sorted", ParameterType.Single, isRequired: false)
    ];

    /// <summary>
    /// Returns (searchCount, resultCount) from the range dimensions.
    /// VLOOKUP searches rows and indexes columns; HLOOKUP is the opposite.
    /// </summary>
    protected abstract (int searchCount, int resultCount) GetSearchAndResultCounts(int rows, int columns);

    /// <summary>
    /// Returns fallback (rows, columns) when the range is not a RangeList.
    /// VLOOKUP assumes a single-column vector; HLOOKUP assumes a single-row vector.
    /// </summary>
    protected abstract (int rows, int columns) GetFallbackDimensions(int count);

    /// <summary>
    /// Returns the flat index into the range for the search cell at position <paramref name="i"/>
    /// along the search axis (row 0 for HLOOKUP, column 0 for VLOOKUP).
    /// </summary>
    protected abstract int GetSearchCellIndex(int i, int columns);

    /// <summary>
    /// Returns the flat index into the range for the result cell given the matched search
    /// position and the 1-based requested index.
    /// </summary>
    protected abstract int GetResultCellIndex(int matchPosition, int requestedIndex, int columns);

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var searchKey = arguments.GetSingle("search_key");
        var rangeArg = arguments.GetRange("range");
        var indexArg = arguments.GetSingle("index");
        var isSortedArg = arguments.GetSingle("is_sorted");

        if (searchKey is null || rangeArg is null || indexArg is null)
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
            (rows, columns) = GetFallbackDimensions(rangeArg.Count);
        }

        if (rows <= 0 || columns <= 0)
        {
            return CellData.FromError(CellError.Value);
        }

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
                var cell = rangeArg[GetSearchCellIndex(i, columns)];
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
                var cell = rangeArg[GetSearchCellIndex(i, columns)];
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

        return rangeArg[GetResultCellIndex(matchPosition, index.Value, columns)];
    }
}
