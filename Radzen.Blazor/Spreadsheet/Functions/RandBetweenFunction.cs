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

        if (!TryToInt(bottomArg, out var bottom) || !TryToInt(topArg, out var top))
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

    private static bool TryToInt(CellData data, out int value)
    {
        value = 0;
        if (data.Type == CellDataType.Number)
        {
            // Truncate toward zero like Excel INT for arguments
            var d = data.GetValueOrDefault<double>();
            value = (int)System.Math.Truncate(d);
            return true;
        }
        if (data.Type == CellDataType.String)
        {
            if (CellData.TryConvertFromString(data.GetValueOrDefault<string>(), out var conv, out var t) && t == CellDataType.Number)
            {
                value = (int)System.Math.Truncate((double)conv!);
                return true;
            }
        }
        if (data.Type == CellDataType.Boolean)
        {
            value = data.GetValueOrDefault<bool>() ? 1 : 0;
            return true;
        }
        return false;
    }
}