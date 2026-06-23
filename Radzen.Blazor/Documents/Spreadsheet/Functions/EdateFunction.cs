#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class EdateFunction : FormulaFunction
{
    public override string Name => "EDATE";

    public override FunctionParameter[] Parameters =>
    [
        new("start_date", ParameterType.Single, isRequired: true),
        new("months", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var startArg = arguments.GetSingle("start_date");

        if (startArg is null || startArg.IsError)
        {
            return startArg ?? CellData.FromError(CellError.Value);
        }

        if (!startArg.TryCoerceToDate(out var startDate))
        {
            return CellData.FromError(CellError.Value);
        }

        if (!TryGetInteger(arguments, "months", true, null, out var months, out var error))
        {
            return error!;
        }

        try
        {
            var result = startDate.AddMonths(months);

            // .NET AddMonths clamps a day past the target month's end (e.g. Jan 31 + 1 month = Feb 28/29).
            return CellData.FromDate(result);
        }
        catch (ArgumentOutOfRangeException)
        {
            return CellData.FromError(CellError.Num);
        }
    }
}
