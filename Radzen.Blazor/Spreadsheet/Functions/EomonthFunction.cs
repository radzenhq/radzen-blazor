#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class EomonthFunction : FormulaFunction
{
    public override string Name => "EOMONTH";

    public override FunctionParameter[] Parameters =>
    [
        new("start_date", ParameterType.Single, isRequired: true),
        new("months", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var startArg = arguments.GetSingle("start_date");

        if (startArg == null || startArg.IsError)
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
            var target = startDate.AddMonths(months);
            var endOfMonth = new DateTime(target.Year, target.Month, DateTime.DaysInMonth(target.Year, target.Month));

            return CellData.FromDate(endOfMonth);
        }
        catch (ArgumentOutOfRangeException)
        {
            return CellData.FromError(CellError.Num);
        }
    }
}
