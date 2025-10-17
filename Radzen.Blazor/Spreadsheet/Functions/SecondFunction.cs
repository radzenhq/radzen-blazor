#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class SecondFunction : DatePartFunctionBase
{
    public override string Name => "SECOND";

    protected override int GetPart(DateTime dateTime) => dateTime.Second;
}