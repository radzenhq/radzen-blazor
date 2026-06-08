#nullable enable

namespace Radzen.Documents.Spreadsheet;

class CodeFunction : FormulaFunction
{
    public override string Name => "CODE";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var arg = arguments.GetSingle("text");

        if (arg is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (arg.IsError)
        {
            return arg;
        }

        var text = arg.ToString() ?? string.Empty;

        if (text.Length == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromNumber(Windows1252.ToCode(text[0]));
    }
}
