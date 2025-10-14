#nullable enable

using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

class LargeFunction : KOrderFunctionBase
{
    public override string Name => "LARGE";

    protected override double GetResult(List<double> numbers, int k)
    {
        return numbers[numbers.Count - k];
    }
}