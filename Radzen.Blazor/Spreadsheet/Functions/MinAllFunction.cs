#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class MinAllFunction : MinMaxAllBase
{
    public override string Name => "MINA";
    protected override bool Satisfies(double candidate, double best) => candidate < best;
}