#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class ChooseFunction : FormulaFunction
{
    public override string Name => "CHOOSE";

    public override FunctionParameter[] Parameters =>
    [
        new("index_num", ParameterType.Single, isRequired: true),
        new("value", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("value");

        if (values == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (!TryGetInteger(arguments, "index_num", isRequired: true, defaultValue: null, out var idx, out var error))
        {
            return error!;
        }

        if (idx < 1 || idx > values.Count)
        {
            return CellData.FromError(CellError.Value);
        }

        return values[idx - 1];
    }
}