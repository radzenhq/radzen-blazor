#nullable enable

namespace Radzen.Documents.Spreadsheet;

class DaysFunction : FormulaFunction
{
    public override string Name => "DAYS";

    public override FunctionParameter[] Parameters =>
    [
        new("end_date", ParameterType.Single, isRequired: true),
        new("start_date", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var end = arguments.GetSingle("end_date");
        var start = arguments.GetSingle("start_date");

        if (end is null || start is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (!end.TryCoerceToDate(out var endDate) || !start.TryCoerceToDate(out var startDate))
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromNumber((endDate.Date - startDate.Date).Days);
    }
}
