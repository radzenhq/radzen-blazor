using System;

#nullable enable

namespace Radzen.Documents.Spreadsheet;

class IndexFunction : FormulaFunction
{
    public override string Name => "INDEX";

    public override FunctionParameter[] Parameters =>
    [
        new("array", ParameterType.Collection, isRequired: true) { IsReference = true },
        new("row_num", ParameterType.Single, isRequired: false),
        new("column_num", ParameterType.Single, isRequired: false),
        new("area_num", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (arguments.GetReference("array") is { } reference)
        {
            return Evaluate(arguments, reference.Rows, reference.Columns, reference.LogicalRows, reference.LogicalColumns, isRange: true, (row, column) => reference[row, column]);
        }

        var array = arguments.GetRange("array");

        if (array is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (array is RangeList range)
        {
            return Evaluate(arguments, range.Rows, range.Columns, range.LogicalRows, range.LogicalColumns, isRange: true, (row, column) => range[row * range.Columns + column]);
        }

        return Evaluate(arguments, array.Count, 1, array.Count, 1, isRange: false, (row, column) => array[row]);
    }

    private static CellData Evaluate(FunctionArguments arguments, int rows, int columns, int logicalRows, int logicalColumns, bool isRange, Func<int, int, CellData> cellAt)
    {
        var rowArg = arguments.GetSingle("row_num");
        var colArg = arguments.GetSingle("column_num");
        var areaArg = arguments.GetSingle("area_num");

        if (rowArg is not null && rowArg.IsError)
        {
            return rowArg;
        }
        if (colArg is not null && colArg.IsError)
        {
            return colArg;
        }
        if (areaArg is not null && areaArg.IsError)
        {
            return areaArg;
        }

        // area selection: only area 1 is supported in this implementation
        if (areaArg is not null)
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

        int? row = null;
        int? col = null;

        if (rowArg is not null)
        {
            if (!rowArg.TryGetInt(out var rowIndex, allowBooleans: false, nonNumericTextAsZero: false))
            {
                return CellData.FromError(CellError.Value);
            }
            row = rowIndex;
        }

        if (colArg is not null)
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

        var isEmpty = rows == 0 || columns == 0;

        // Support row==0 or col==0 returning entire column/row reference respectively
        if (rIndex == 0 && cIndex == 0)
        {
            return isEmpty ? CellData.FromError(CellError.Ref) : cellAt(0, 0);
        }

        if (rIndex < 0 || cIndex < 0)
        {
            return CellData.FromError(CellError.Ref);
        }

        if (rIndex > logicalRows || cIndex > logicalColumns)
        {
            return CellData.FromError(CellError.Ref);
        }

        // Entire column
        if (rIndex == 0 && cIndex >= 1)
        {
            if (cIndex > columns)
            {
                return CellData.Empty;
            }

            return isEmpty ? CellData.FromError(CellError.Ref) : cellAt(0, cIndex - 1);
        }

        // Entire row
        if (cIndex == 0 && rIndex >= 1)
        {
            if (!isRange)
            {
                return CellData.FromError(CellError.Ref);
            }

            if (rIndex > rows)
            {
                return CellData.Empty;
            }

            return isEmpty ? CellData.FromError(CellError.Ref) : cellAt(rIndex - 1, 0);
        }

        if (rIndex > rows || cIndex > columns)
        {
            return CellData.Empty;
        }

        return cellAt(rIndex - 1, cIndex - 1);
    }
}
