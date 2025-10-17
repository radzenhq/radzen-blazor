#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class DayFunction : DatePartFunctionBase
{
    public override string Name => "DAY";

    protected override int GetPart(DateTime dateTime) => dateTime.Day;
}