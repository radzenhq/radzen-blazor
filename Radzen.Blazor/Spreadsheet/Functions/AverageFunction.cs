#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class AverageFunction : FormulaFunction
{
    public override FunctionParameter[] Parameters =>
    [
        new ("number", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var numbers = arguments.GetSequence("number");

        if (numbers == null || numbers.Count == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        var sum = 0d;
        var count = 0;

        foreach (var argument in numbers)
        {
            if (argument.IsError)
            {
                return argument;
            }

            if (argument.IsEmpty || argument.Type != CellDataType.Number)
            {
                continue;
            }

            var value = argument.GetValueOrDefault<double>();

            sum += value;
            count++;
        }

        if (count == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        return CellData.FromNumber(sum / count);
    }
}