#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class DayFunction : FormulaFunction
{
    public override string Name => "DAY";

    public override FunctionParameter[] Parameters =>
    [
        new("serial_number", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var arg = arguments.GetSingle("serial_number");
        if (arg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (arg.IsError)
        {
            return arg;
        }

        // Accept date value or numeric serial (including fractional)
        if (arg.Type == CellDataType.Date)
        {
            var dt = arg.GetValueOrDefault<DateTime>();
            return CellData.FromNumber(dt.Day);
        }

        if (arg.TryCoerceToNumber(out var serial, allowBooleans: false, nonNumericTextAsZero: false))
        {
            var dt = serial.ToDate();
            return CellData.FromNumber(dt.Day);
        }

        return CellData.FromError(CellError.Value);
    }
}