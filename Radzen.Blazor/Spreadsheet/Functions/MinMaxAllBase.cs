#nullable enable

namespace Radzen.Blazor.Spreadsheet;

abstract class MinMaxAllBase : FormulaFunction
{
    protected abstract CellData Compute(System.Collections.Generic.List<double> numbers);

    public override FunctionParameter[] Parameters =>
    [
        new ("value", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("value");

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

            if (value.IsEmpty)
            {
                continue;
            }

            if (value.TryCoerceToNumber(out var num, allowBooleans: true, nonNumericTextAsZero: true))
            {
                numeric.Add(num);
            }
        }

        if (numeric.Count == 0)
        {
            return CellData.FromNumber(0d);
        }

        return Compute(numeric);
    }
}