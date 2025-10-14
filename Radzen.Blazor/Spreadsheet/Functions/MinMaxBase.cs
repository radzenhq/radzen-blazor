#nullable enable

namespace Radzen.Blazor.Spreadsheet;

abstract class MinMaxBase : FormulaFunction
{
    protected abstract CellData Compute(System.Collections.Generic.List<double> numbers);

    public override FunctionParameter[] Parameters =>
    [
        new ("number", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("number");

        if (values == null)
        {
            return CellData.FromNumber(0d);
        }

        var numeric = new System.Collections.Generic.List<double>();

        foreach (var value in values)
        {
            if (value.IsError)
            {
                return value;
            }

            if (value.Type == CellDataType.Number)
            {
                numeric.Add(value.GetValueOrDefault<double>());
            }
            else if (value.Type == CellDataType.String)
            {
                // Include numeric string literals (typed directly)
                if (CellData.TryConvertFromString(value.GetValueOrDefault<string>(), out var converted, out var valueType) && valueType == CellDataType.Number)
                {
                    numeric.Add((double)converted!);
                }
            }
        }

        if (numeric.Count == 0)
        {
            return CellData.FromNumber(0d);
        }

        return Compute(numeric);
    }
}