#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class MinFunction : MinMaxBase
{
    public override string Name => "MIN";
    protected override bool Satisfies(double candidate, double best) => candidate < best;
}