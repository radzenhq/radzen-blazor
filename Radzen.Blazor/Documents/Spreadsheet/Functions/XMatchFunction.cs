#nullable enable

namespace Radzen.Documents.Spreadsheet;

class XMatchFunction : FormulaFunction
{
    public override string Name => "XMATCH";

    public override FunctionParameter[] Parameters =>
    [
        new("lookup_value", ParameterType.Single, isRequired: true),
        new("lookup_array", ParameterType.Collection, isRequired: true),
        new("match_mode", ParameterType.Single, isRequired: false),
        // search_mode is accepted for signature compatibility; the engine always searches first-to-last,
        // which matches Excel on the sorted input that approximate modes assume.
        new("search_mode", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var lookup = arguments.GetSingle("lookup_value");
        var array = arguments.GetRange("lookup_array");

        if (lookup is null || array is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (lookup.IsError)
        {
            return lookup;
        }

        if (!TryGetInteger(arguments, "match_mode", isRequired: false, defaultValue: 0, out var matchMode, out var matchError))
        {
            return matchError!;
        }

        var (mode, wildcards) = matchMode switch
        {
            0 => (0, false),
            -1 => (-1, false),
            1 => (1, false),
            2 => (0, true),
            _ => (int.MinValue, false)
        };

        if (mode == int.MinValue)
        {
            return CellData.FromError(CellError.Value);
        }

        var index = RangeSearch.Find(array, lookup, mode, wildcards, out var error);

        if (error is not null)
        {
            return error;
        }

        return index >= 0 ? CellData.FromNumber(index + 1) : CellData.FromError(CellError.NA);
    }
}
