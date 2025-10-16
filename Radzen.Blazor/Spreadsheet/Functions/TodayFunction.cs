#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class TodayFunction : FormulaFunction
{
    public override string Name => "TODAY";

    public override FunctionParameter[] Parameters => [];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var today = DateTime.Today;
        return CellData.FromDate(today);
    }
}