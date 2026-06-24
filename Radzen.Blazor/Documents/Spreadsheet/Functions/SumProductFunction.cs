#nullable enable

namespace Radzen.Documents.Spreadsheet;

class SumProductFunction : FormulaFunction
{
    public override string Name => "SUMPRODUCT";

    public override FunctionParameter[] Parameters =>
    [
        new("arrays", ParameterType.Group, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var arrays = arguments.GetGroups("arrays");

        if (arrays is null || arrays.Count == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        var count = arrays[0].Count;

        foreach (var array in arrays)
        {
            if (array.Count != count)
            {
                return CellData.FromError(CellError.Value);
            }
        }

        var sum = 0d;

        for (var i = 0; i < count; i++)
        {
            var product = 1d;

            foreach (var array in arrays)
            {
                var cell = array[i];

                if (cell.IsError)
                {
                    return cell;
                }

                // Non-numeric entries count as 0.
                product *= cell.Type == CellDataType.Number ? cell.GetValueOrDefault<double>() : 0d;
            }

            sum += product;
        }

        return CellData.FromNumber(sum);
    }
}
