using System;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class SumIfFunction : FormulaFunction
{
    public override string Name => "SUMIF";

    public override FunctionParameter[] Parameters =>
    [
        new("range", ParameterType.Collection, isRequired: true),
        new("criteria", ParameterType.Single, isRequired: true),
        new("sum_range", ParameterType.Collection, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var range = arguments.GetRange("range");

        if (range == null)
        {
            return CellData.FromError(CellError.Value);
        }

        var criteria = arguments.GetSingle("criteria");

        if (criteria == null)
        {
            return CellData.FromError(CellError.Value);
        }

        var sumRange = arguments.GetRange("sum_range");
        
        var actualSumRange = sumRange ?? range;

        if (range.Count != actualSumRange.Count)
        {
            return CellData.FromError(CellError.Value);
        }

        var sum = 0d;

        for (int i = 0; i < range.Count; i++)
        {
            var rangeCell = range[i];
            var sumCell = actualSumRange[i];

            // Skip if range cell has an error
            if (rangeCell.IsError)
            {
                return rangeCell;
            }

            // Skip if sum cell has an error
            if (sumCell.IsError)
            {
                return sumCell;
            }

            // Check if the range cell matches the criteria
            if (rangeCell.MatchesCriteria(criteria))
            {
                // Add the corresponding sum cell value if it's a number
                if (sumCell.Type == CellDataType.Number)
                {
                    sum += sumCell.GetValueOrDefault<double>();
                }
                // If sum cell is empty, treat as 0
                else if (sumCell.IsEmpty)
                {
                    // Do nothing, effectively adding 0
                }
                // If sum cell is not a number and not empty, skip it
            }
        }

        return CellData.FromNumber(sum);
    }

}