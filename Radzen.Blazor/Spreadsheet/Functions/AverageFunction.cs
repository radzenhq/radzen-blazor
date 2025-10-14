#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class AverageFunction : FormulaFunction
{
    public override string Name => "AVERAGE";

    public override FunctionParameter[] Parameters =>
    [
        new ("number", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("number");

        if (values == null || values.Count == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        var numeric = new System.Collections.Generic.List<double>();

        foreach (var argument in values)
        {
            if (argument.IsError)
            {
                return argument;
            }

            if (argument.IsEmpty || argument.Type != CellDataType.Number)
            {
                continue;
            }

            numeric.Add(argument.GetValueOrDefault<double>());
        }

        return AggregationMethods.Average(numeric);
    }
}