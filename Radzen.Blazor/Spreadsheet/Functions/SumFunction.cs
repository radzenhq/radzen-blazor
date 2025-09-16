#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class SumFunction : FormulaFunction
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
            return CellData.FromError(CellError.Value);
        }

        var sum = 0d;

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
        }

        return CellData.FromNumber(sum);
    }
}