#nullable enable

namespace Radzen.Documents.Spreadsheet;

class TimeFunction : FormulaFunction
{
    public override string Name => "TIME";

    public override FunctionParameter[] Parameters =>
    [
        new("hour", ParameterType.Single, isRequired: true),
        new("minute", ParameterType.Single, isRequired: true),
        new("second", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetInteger(arguments, "hour", true, null, out var hour, out var error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "minute", true, null, out var minute, out var error2))
        {
            return error2!;
        }

        if (!TryGetInteger(arguments, "second", true, null, out var second, out var error3))
        {
            return error3!;
        }

        var totalSeconds = hour * 3600 + minute * 60 + second;

        // Excel TIME wraps past 24 hours (86400 seconds per day); double modulo keeps negatives positive.
        totalSeconds = ((totalSeconds % 86400) + 86400) % 86400;

        return CellData.FromNumber(totalSeconds / 86400.0);
    }
}
