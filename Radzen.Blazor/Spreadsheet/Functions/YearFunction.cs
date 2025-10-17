#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class YearFunction : DatePartFunctionBase
{
    public override string Name => "YEAR";

    protected override int GetPart(DateTime dateTime) => dateTime.Year;
}