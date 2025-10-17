#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class MonthFunction : DatePartFunctionBase
{
    public override string Name => "MONTH";

    protected override int GetPart(DateTime dateTime) => dateTime.Month;
}