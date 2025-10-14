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

            switch (value.Type)
            {
                case CellDataType.Number:
                    numeric.Add(value.GetValueOrDefault<double>());
                    break;
                case CellDataType.Boolean:
                    numeric.Add(value.GetValueOrDefault<bool>() ? 1d : 0d);
                    break;
                case CellDataType.String:
                    if (CellData.TryConvertFromString(value.GetValueOrDefault<string>(), out var converted, out var valueType) && valueType == CellDataType.Number)
                    {
                        numeric.Add((double)converted!);
                    }
                    else
                    {
                        numeric.Add(0d);
                    }
                    break;
                default:
                    break;
            }
        }

        if (numeric.Count == 0)
        {
            return CellData.FromNumber(0d);
        }

        return Compute(numeric);
    }
}