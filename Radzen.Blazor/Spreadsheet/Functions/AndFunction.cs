#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class AndFunction : FormulaFunction
{
    public override string Name => "AND";

    public override FunctionParameter[] Parameters =>
    [
        new ("logical", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var logicals = arguments.GetSequence("logical");

        if (logicals == null || logicals.Count == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        bool? result = null;

        foreach (var argument in logicals)
        {
            if (argument.IsError)
            {
                return argument;
            }

            if (argument.IsEmpty)
            {
                continue;
            }

            var value = argument.GetValueOrDefault<bool?>();

            if (value is null && result is null)
            {
                continue;
            }

            result ??= true;

            result &= value;
        }

        if (result is null)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromBoolean(result.Value);
    }
}