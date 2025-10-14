#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class RandBetweenFunction : FormulaFunction
{
    public override string Name => "RANDBETWEEN";

    public override FunctionParameter[] Parameters =>
    [
        new("bottom", ParameterType.Single, isRequired: true),
        new("top", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var bottomArg = arguments.GetSingle("bottom");
        var topArg = arguments.GetSingle("top");

        if (bottomArg == null || topArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (bottomArg.IsError) 
        {
            return bottomArg;
        }

        if (topArg.IsError)
        {
            return topArg;
        }

        if (!bottomArg.TryGetInt(out var bottom) || !topArg.TryGetInt(out var top))
        {
            return CellData.FromError(CellError.Value);
        }

        if (bottom > top)
        {
            return CellData.FromError(CellError.Num);
        }

        var range = top - bottom + 1;
        int result = bottom + System.Random.Shared.Next(range);
        return CellData.FromNumber(result);
    }

    // Number parsing moved to CellData.TryGetInt
}