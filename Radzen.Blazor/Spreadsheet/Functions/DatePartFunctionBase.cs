#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

abstract class DatePartFunctionBase : FormulaFunction
{
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

        DateTime dt;

        if (arg.Type == CellDataType.Date)
        {
            dt = arg.GetValueOrDefault<DateTime>();
        }
        else if (arg.TryCoerceToNumber(out var serial, allowBooleans: false, nonNumericTextAsZero: false))
        {
            dt = serial.ToDate();
        }
        else
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromNumber(GetPart(dt));
    }

    protected abstract int GetPart(DateTime dateTime);
}