#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class XLookupFunction : FormulaFunction
{
    public override string Name => "XLOOKUP";

    public override FunctionParameter[] Parameters =>
    [
        new("lookup_value", ParameterType.Single, isRequired: false),
        new("lookup_array", ParameterType.Collection, isRequired: true),
        new("return_array", ParameterType.Collection, isRequired: true),
        new("if_not_found", ParameterType.Single, isRequired: false),
        new("match_mode", ParameterType.Single, isRequired: false),
        new("search_mode", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var lookupValueArg = arguments.GetSingle("lookup_value");
        var lookupArray = arguments.GetRange("lookup_array");
        var returnArray = arguments.GetRange("return_array");
        var ifNotFound = arguments.GetSingle("if_not_found");
        var matchModeArg = arguments.GetSingle("match_mode");
        var searchModeArg = arguments.GetSingle("search_mode");

        if (lookupArray == null || returnArray == null)
        {
            return CellData.FromError(CellError.Value);
        }

        // Treat missing lookup_value as empty value (search for empty cells)
        var lookupValue = lookupValueArg ?? new CellData(null);

        if (lookupValue.IsError)
        {
            return lookupValue;
        }

        // Validate arrays are 1D (either a single row or a single column)
        int lookupRows = 1, lookupCols = lookupArray.Count;
        if (lookupArray is RangeList lrl)
        {
            lookupRows = lrl.Rows;
            lookupCols = lrl.Columns;
        }

        int returnRows = 1, returnCols = returnArray.Count;
        if (returnArray is RangeList rrl)
        {
            returnRows = rrl.Rows;
            returnCols = rrl.Columns;
        }

        var lookupIsRow = lookupRows == 1 && lookupCols >= 1;
        var lookupIsCol = lookupCols == 1 && lookupRows >= 1;
        var returnIsRow = returnRows == 1 && returnCols >= 1;
        var returnIsCol = returnCols == 1 && returnRows >= 1;

        if (!(lookupIsRow || lookupIsCol) || !(returnIsRow || returnIsCol))
        {
            return CellData.FromError(CellError.Value);
        }

        // Linearized length must match
        var lookupLength = lookupRows * lookupCols;
        var returnLength = returnRows * returnCols;
        if (lookupLength != returnLength)
        {
            return CellData.FromError(CellError.Value);
        }

        // Parse modes
        var matchMode = matchModeArg?.GetValueOrDefault<int?>() ?? 0; // 0 default
        var searchMode = searchModeArg?.GetValueOrDefault<int?>() ?? 1; // 1 default

        // Wildcard matching only applies when match_mode == 2 and lookup_value is string
        bool useWildcard = matchMode == 2 && lookupValue.Type == CellDataType.String;

        int index = -1;

        // Binary search only for match_mode in {0,-1,1} and when requested
        bool canBinary = !useWildcard && (matchMode == 0 || matchMode == -1 || matchMode == 1) && (searchMode == 2 || searchMode == -2);

        if (canBinary)
        {
            // Binary search assumes sorted lookupArray according to searchMode (ascending for 2, descending for -2)
            bool ascending = searchMode == 2;
            index = BinarySearch(lookupArray, lookupRows, lookupCols, lookupValue, matchMode, ascending);
        }
        else
        {
            index = LinearSearch(lookupArray, lookupRows, lookupCols, lookupValue, matchMode, searchMode, useWildcard);
        }

        if (index == -1)
        {
            if (ifNotFound != null)
            {
                return ifNotFound;
            }
            return CellData.FromError(CellError.NA);
        }

        var resultCell = returnArray[index];
        return resultCell;
    }

    private static int LinearSearch(System.Collections.Generic.List<CellData> lookupArray, int rows, int columns, CellData lookupValue, int matchMode, int searchMode, bool useWildcard)
    {
        int start, end, step;
        if (searchMode == -1)
        {
            start = rows * columns - 1; end = -1; step = -1;
        }
        else
        {
            start = 0; end = rows * columns; step = 1;
        }

        int bestIndex = -1;
        for (int i = start; i != end; i += step)
        {
            var cell = lookupArray[i];
            if (cell.IsError)
            {
                return -1; // propagate error by signaling not found; caller will handle if_not_found or NA; VLOOKUP propagated error, but here we can't return CellData from here
            }

            bool equal;
            if (useWildcard)
            {
                // Treat lookupValue string as criteria pattern
                equal = cell.MatchesCriteria(lookupValue);
            }
            else
            {
                equal = cell.IsEqualTo(lookupValue);
            }

            if (matchMode == 0 || useWildcard)
            {
                if (equal)
                {
                    return i;
                }
            }
            else if (matchMode == -1)
            {
                if (equal)
                {
                    return i;
                }
                if (cell.IsLessThan(lookupValue) || cell.IsEqualTo(lookupValue))
                {
                    // Track last <= lookupValue in the chosen traversal direction
                    bestIndex = i;
                }
            }
            else if (matchMode == 1)
            {
                if (equal)
                {
                    return i;
                }
                // For next larger, when traversing forward, pick first > value (if reverse, still first in traversal order)
                if (cell.IsGreaterThan(lookupValue))
                {
                    return i;
                }
            }
        }

        return bestIndex;
    }

    private static int BinarySearch(System.Collections.Generic.List<CellData> lookupArray, int rows, int columns, CellData lookupValue, int matchMode, bool ascending)
    {
        int lo = 0, hi = rows * columns - 1;
        int found = -1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            var cell = lookupArray[mid];
            if (cell.IsError)
            {
                return -1;
            }

            int cmp;
            if (cell.IsEqualTo(lookupValue))
            {
                found = mid;
                // For consistency, return the first occurrence to the left
                hi = mid - 1;
                continue;
            }
            else if (cell.IsLessThan(lookupValue))
            {
                cmp = -1;
            }
            else if (cell.IsGreaterThan(lookupValue))
            {
                cmp = 1;
            }
            else
            {
                // incomparable types
                cmp = 0;
            }

            if (ascending)
            {
                if (cmp < 0) lo = mid + 1; else hi = mid - 1;
            }
            else
            {
                if (cmp > 0) lo = mid + 1; else hi = mid - 1;
            }
        }

        if (found != -1)
        {
            return found;
        }

        // No exact match; handle approximate
        if (matchMode == -1)
        {
            // next smaller item: in ascending, hi will be index of last <; in descending, lo is last < due to reversed order
            int candidate = ascending ? hi : lo;
            return (candidate >= 0 && candidate < rows * columns) ? candidate : -1;
        }
        if (matchMode == 1)
        {
            // next larger item: in ascending, lo is first >; in descending, hi is first > due to reversed
            int candidate = ascending ? lo : hi;
            return (candidate >= 0 && candidate < rows * columns) ? candidate : -1;
        }

        return -1;
    }
}


