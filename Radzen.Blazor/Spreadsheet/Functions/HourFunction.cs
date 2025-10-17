#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class HourFunction : DatePartFunctionBase
{
    public override string Name => "HOUR";

    protected override int GetPart(DateTime dateTime) => dateTime.Hour;
}