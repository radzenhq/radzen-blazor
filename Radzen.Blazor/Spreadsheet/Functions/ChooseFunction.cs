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
        var indexArg = arguments.GetSingle("index_num");
        var values = arguments.GetSequence("value");

        if (indexArg == null || values == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (indexArg.IsError)
        {
            return indexArg;
        }

        if (!indexArg.TryGetInt(out var idx, allowBooleans: false, nonNumericTextAsZero: false))
        {
            return CellData.FromError(CellError.Value);
        }

        if (idx < 1 || idx > values.Count)
        {
            return CellData.FromError(CellError.Value);
        }

        var choice = values[idx - 1];

        return choice;
    }
}