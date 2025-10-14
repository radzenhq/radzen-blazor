#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class MaxAllFunction : MinMaxAllBase
{
    public override string Name => "MAXA";
    protected override bool Satisfies(double candidate, double best) => candidate > best;
}