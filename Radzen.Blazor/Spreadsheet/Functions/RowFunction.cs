#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class RowFunction : FormulaFunction
{
    public override string Name => "ROW";

    public override FunctionParameter[] Parameters =>
    [
        new("reference", ParameterType.Collection, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var reference = arguments.GetRange("reference");

        if (reference == null)
        {
            // No reference provided: use current cell
            return CellData.FromNumber(arguments.CurrentCell.Address.Row + 1);
        }

        if (reference is RangeList range)
        {
            // If the range spans multiple rows AND multiple columns, it's invalid for ROW
            if (range.Rows > 1 && range.Columns > 1)
            {
                return CellData.FromError(CellError.Value);
            }

            // Otherwise (single row or single column) return the top-left row index
            return CellData.FromNumber(range.StartRow + 1);
        }

        // Single value (treated as 1x1 reference), we cannot resolve its address → error
        return CellData.FromError(CellError.Value);
    }
}

