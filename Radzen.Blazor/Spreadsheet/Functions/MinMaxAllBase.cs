#nullable enable

namespace Radzen.Blazor.Spreadsheet;

abstract class MinMaxAllBase : FormulaFunction
{
    protected abstract bool Satisfies(double candidate, double best);

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

        var hasAny = false;
        double best = 0d;

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

            double? candidate = null;

            switch (value.Type)
            {
                case CellDataType.Number:
                    candidate = value.GetValueOrDefault<double>();
                    break;
                case CellDataType.Boolean:
                    candidate = value.GetValueOrDefault<bool>() ? 1d : 0d;
                    break;
                case CellDataType.String:
                    if (CellData.TryConvertFromString(value.GetValueOrDefault<string>(), out var converted, out var valueType) && valueType == CellDataType.Number)
                    {
                        candidate = (double)converted!;
                    }
                    else
                    {
                        candidate = 0d;
                    }
                    break;
                default:
                    break;
            }

            if (candidate is null)
            {
                continue;
            }

            if (!hasAny)
            {
                best = candidate.Value;
                hasAny = true;
            }
            else if (Satisfies(candidate.Value, best))
            {
                best = candidate.Value;
            }
        }

        return CellData.FromNumber(hasAny ? best : 0d);
    }
}