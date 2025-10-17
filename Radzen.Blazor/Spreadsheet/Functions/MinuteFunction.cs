#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class MinuteFunction : DatePartFunctionBase
{
    public override string Name => "MINUTE";

    protected override int GetPart(DateTime dateTime) => dateTime.Minute;
}