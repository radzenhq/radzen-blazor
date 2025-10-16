#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class FindFunction : FormulaFunction
{
    public override string Name => "FIND";

    public override FunctionParameter[] Parameters =>
    [
        new("find_text", ParameterType.Single, isRequired: true),
        new("within_text", ParameterType.Single, isRequired: true),
        new("start_num", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "find_text", out var findText, out var error))
        {
            return error!;
        }

        if (!TryGetString(arguments, "within_text", out var withinText, out error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "start_num", isRequired: false, defaultValue: 1, out var startNum, out error))
        {
            return error!;
        }

        if (startNum <= 0 || startNum > withinText.Length)
        {
            return CellData.FromError(CellError.Value);
        }

        var startIndex = startNum - 1;

        // Empty findText returns start_num
        if (findText.Length == 0)
        {
            return CellData.FromNumber(startNum);
        }

        // Case-sensitive, no wildcards: use Ordinal comparison
        var pos = withinText.IndexOf(findText, startIndex, System.StringComparison.Ordinal);
        if (pos < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromNumber(pos + 1);
    }
}