#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class WeekdayFunction : FormulaFunction
{
    public override string Name => "WEEKDAY";

    public override FunctionParameter[] Parameters =>
    [
        new("serial_number", ParameterType.Single, isRequired: true),
        new("return_type", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var serialArg = arguments.GetSingle("serial_number");
        if (serialArg == null)
        {
            return CellData.FromError(CellError.Value);
        }
        if (serialArg.IsError)
        {
            return serialArg;
        }

        if (!serialArg.TryCoerceToDate(out var dateTime))
        {
            return CellData.FromError(CellError.Value);
        }

        // Determine return_type (default 1)
        var returnType = 1;
        var returnTypeArg = arguments.GetSingle("return_type");
        if (returnTypeArg != null)
        {
            if (returnTypeArg.IsError)
            {
                return returnTypeArg;
            }
            if (!returnTypeArg.TryGetInt(out returnType, allowBooleans: true, nonNumericTextAsZero: false))
            {
                return CellData.FromError(CellError.Value);
            }
        }

        // Excel supports specific return types only; validate range
        if (returnType != 1 && returnType != 2 && returnType != 3 && (returnType < 11 || returnType > 17))
        {
            return CellData.FromError(CellError.Num);
        }

        // Compute base weekday: Sunday=1..Saturday=7 (Excel default)
        // In .NET, DayOfWeek: Sunday=0..Saturday=6
        var dow = (int)dateTime.DayOfWeek; // 0..6
        var sundayBased = dow + 1; // 1..7 with Sunday=1

        var result = returnType switch
        {
            1 => sundayBased,
            2 or 11 => // Monday=1..Sunday=7
                dow == 0 ? 7 : dow, // Sunday -> 7, Monday(1)..Saturday(6)
            3 => // Monday=0..Sunday=6
                dow == 0 ? 6 : dow - 1,
            12 => // Tuesday=1..Monday=7
                ((dow - (int)DayOfWeek.Tuesday + 7) % 7) + 1,
            13 => // Wednesday=1..Tuesday=7
                ((dow - (int)DayOfWeek.Wednesday + 7) % 7) + 1,
            14 => // Thursday=1..Wednesday=7
                ((dow - (int)DayOfWeek.Thursday + 7) % 7) + 1,
            15 => // Friday=1..Thursday=7
                ((dow - (int)DayOfWeek.Friday + 7) % 7) + 1,
            16 => // Saturday=1..Friday=7
                ((dow - (int)DayOfWeek.Saturday + 7) % 7) + 1,
            17 => // Sunday=1..Saturday=7
                sundayBased,
            _ => sundayBased
        };

        return CellData.FromNumber(result);
    }
}