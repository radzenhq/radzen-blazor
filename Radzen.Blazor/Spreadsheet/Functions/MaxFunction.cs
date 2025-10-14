#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class MaxFunction : MinMaxBase
{
    public override string Name => "MAX";
    protected override bool Satisfies(double candidate, double best) => candidate > best;
}