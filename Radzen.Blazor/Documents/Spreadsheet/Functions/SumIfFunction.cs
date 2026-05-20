#nullable enable

namespace Radzen.Documents.Spreadsheet;

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

        if (range is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var criteria = arguments.GetSingle("criteria");

        if (criteria is null)
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

            if (rangeCell.IsError)
            {
                return rangeCell;
            }

            if (sumCell.IsError)
            {
                return sumCell;
            }

            if (rangeCell.MatchesCriteria(criteria))
            {
                if (sumCell.Type == CellDataType.Number)
                {
                    sum += sumCell.GetValueOrDefault<double>();
                }
                // If sum cell is empty, treat as 0
                else if (sumCell.IsEmpty)
                {
                }
                // If sum cell is not a number and not empty, skip it
            }
        }

        return CellData.FromNumber(sum);
    }
}