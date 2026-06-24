#nullable enable

using System;
using System.Globalization;

namespace Radzen.Documents.Spreadsheet;

class DateValueFunction : FormulaFunction
{
    public override string Name => "DATEVALUE";

    public override FunctionParameter[] Parameters =>
    [
        new("date_text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var arg = arguments.GetSingle("date_text");

        if (arg is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (arg.IsError)
        {
            return arg;
        }

        if (arg.Type != CellDataType.String)
        {
            return CellData.FromError(CellError.Value);
        }

        var text = arg.GetValueOrDefault<string>() ?? string.Empty;

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return CellData.FromDate(date.Date);
        }

        return CellData.FromError(CellError.Value);
    }
}
