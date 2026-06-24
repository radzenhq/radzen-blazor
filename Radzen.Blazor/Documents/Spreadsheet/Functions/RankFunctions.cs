#nullable enable

namespace Radzen.Documents.Spreadsheet;

class RankFunction : FormulaFunction
{
    public override string Name => "RANK";

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true),
        new("ref", ParameterType.Collection, isRequired: true),
        new("order", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetNumber(arguments, "number", isRequired: true, defaultValue: null, out var number, out var numberError))
        {
            return numberError!;
        }

        var reference = arguments.GetRange("ref");

        if (reference is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (!TryGetInteger(arguments, "order", isRequired: false, defaultValue: 0, out var order, out var orderError))
        {
            return orderError!;
        }

        var rank = 1;
        var found = false;

        foreach (var cell in reference)
        {
            if (cell.IsError)
            {
                return cell;
            }

            if (cell.Type != CellDataType.Number)
            {
                continue;
            }

            var value = cell.GetValueOrDefault<double>();

            if (value == number)
            {
                found = true;
            }

            // order 0/omitted = descending (largest is rank 1); non-zero = ascending.
            if (order == 0 ? value > number : value < number)
            {
                rank++;
            }
        }

        return found ? CellData.FromNumber(rank) : CellData.FromError(CellError.NA);
    }
}
