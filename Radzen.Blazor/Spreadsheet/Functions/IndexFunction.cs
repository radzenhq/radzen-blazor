#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class IndexFunction : FormulaFunction
{
    public override string Name => "INDEX";

    public override FunctionParameter[] Parameters =>
    [
        new("array", ParameterType.Collection, isRequired: true),
        new("row_num", ParameterType.Single, isRequired: false),
        new("column_num", ParameterType.Single, isRequired: false),
        new("area_num", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var array = arguments.GetRange("array");
        var rowArg = arguments.GetSingle("row_num");
        var colArg = arguments.GetSingle("column_num");
        var areaArg = arguments.GetSingle("area_num");

        if (array == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (rowArg != null && rowArg.IsError)
        {
            return rowArg;
        }
        if (colArg != null && colArg.IsError)
        {
            return colArg;
        }
        if (areaArg != null && areaArg.IsError)
        {
            return areaArg;
        }

        // Determine array dimensions
        int rows;
        int cols;
        if (array is RangeList rl)
        {
            rows = rl.Rows;
            cols = rl.Columns;
        }
        else
        {
            rows = array.Count;
            cols = 1;
        }

        // area selection: only area 1 is supported in this implementation
        if (areaArg != null)
        {
            if (!areaArg.TryGetInt(out var areaIndex, allowBooleans: false, nonNumericTextAsZero: false))
            {
                return CellData.FromError(CellError.Value);
            }
            var area = areaIndex;
            if (area != 1)
            {
                return CellData.FromError(CellError.Value);
            }
        }

        // Parse row/col indices. If both omitted -> error
        int? row = null;
        int? col = null;

        if (rowArg != null)
        {
            if (!rowArg.TryGetInt(out var rowIndex, allowBooleans: false, nonNumericTextAsZero: false))
            {
                return CellData.FromError(CellError.Value);
            }
            row = rowIndex;
        }

        if (colArg != null)
        {
            if (!colArg.TryGetInt(out var colIndex, allowBooleans: false, nonNumericTextAsZero: false))
            {
                return CellData.FromError(CellError.Value);
            }
            col = colIndex;
        }

        if (row is null && col is null)
        {
            return CellData.FromError(CellError.Value);
        }

        // Default missing index to 1 (first row/column) to align with common use
        var rIndex = row ?? 1;
        var cIndex = col ?? 1;

        // Support row==0 or col==0 returning entire column/row reference respectively
        if (rIndex == 0 && cIndex == 0)
        {
            return array.Count > 0 ? array[0] : CellData.FromError(CellError.Ref);
        }

        if (rIndex < 0 || cIndex < 0)
        {
            return CellData.FromError(CellError.Ref);
        }

        if (rIndex > rows || cIndex > cols)
        {
            return CellData.FromError(CellError.Ref);
        }

        // Entire column
        if (rIndex == 0 && cIndex >= 1)
        {
            if (array is RangeList arr)
            {
                var startRow = arr.StartRow;
                var startCol = arr.StartColumn + (cIndex - 1);
                var result = new RangeList(arr.Rows, 1, startRow, startCol, arr.Sheet);
                for (int i = 0; i < arr.Rows; i++)
                {
                    var idx = i * arr.Columns + (cIndex - 1);
                    result.Add(array[idx]);
                }
                return result.Count > 0 ? result[0] : CellData.FromError(CellError.Ref);
            }
            // Fallback: build linear column from list
            if (cols == 1 && cIndex == 1)
            {
                return array.Count > 0 ? array[0] : CellData.FromError(CellError.Ref);
            }
            return CellData.FromError(CellError.Ref);
        }

        // Entire row
        if (cIndex == 0 && rIndex >= 1)
        {
            if (array is RangeList arr)
            {
                var startRow = arr.StartRow + (rIndex - 1);
                var startCol = arr.StartColumn;
                var result = new RangeList(1, arr.Columns, startRow, startCol, arr.Sheet);
                for (int j = 0; j < arr.Columns; j++)
                {
                    var idx = (rIndex - 1) * arr.Columns + j;
                    result.Add(array[idx]);
                }
                return result.Count > 0 ? result[0] : CellData.FromError(CellError.Ref);
            }
            return CellData.FromError(CellError.Ref);
        }

        var flatIndex = (rIndex - 1) * cols + (cIndex - 1);
        return array[flatIndex];
    }
}