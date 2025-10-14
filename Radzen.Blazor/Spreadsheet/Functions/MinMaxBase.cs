#nullable enable

namespace Radzen.Blazor.Spreadsheet;

abstract class MinMaxBase : FormulaFunction
{
    protected abstract bool Satisfies(double candidate, double best);

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

        var hasNumber = false;
        double best = 0d;

        foreach (var value in values)
        {
            if (value.IsError)
            {
                return value;
            }

            double? maybeNum = null;

            if (value.Type == CellDataType.Number)
            {
                maybeNum = value.GetValueOrDefault<double>();
            }
            else if (value.Type == CellDataType.String)
            {
                // Count numeric string literals
                if (CellData.TryConvertFromString(value.GetValueOrDefault<string>(), out var converted, out var valueType) && valueType == CellDataType.Number)
                {
                    maybeNum = (double)converted!;
                }
            }

            if (maybeNum is null)
            {
                continue;
            }

            var num = maybeNum.Value;

            if (!hasNumber)
            {
                best = num;
                hasNumber = true;
            }
            else if (Satisfies(num, best))
            {
                best = num;
            }
        }

        return hasNumber ? CellData.FromNumber(best) : CellData.FromNumber(0d);
    }
}