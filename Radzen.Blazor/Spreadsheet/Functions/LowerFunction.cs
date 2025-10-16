#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class LowerFunction : FormulaFunction
{
    public override string Name => "LOWER";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "text", out var text, out var error))
        {
            return error!;
        }

        return CellData.FromString(text.ToLowerInvariant());
    }
}