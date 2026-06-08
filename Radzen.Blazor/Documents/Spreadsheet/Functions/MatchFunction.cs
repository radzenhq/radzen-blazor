#nullable enable

namespace Radzen.Documents.Spreadsheet;

class MatchFunction : FormulaFunction
{
    public override string Name => "MATCH";

    public override FunctionParameter[] Parameters =>
    [
        new("lookup_value", ParameterType.Single, isRequired: true),
        new("lookup_array", ParameterType.Collection, isRequired: true),
        new("match_type", ParameterType.Single, isRequired: false)
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

        if (!TryGetInteger(arguments, "match_type", isRequired: false, defaultValue: 1, out var matchType, out var error))
        {
            return error!;
        }

        // MATCH type 1 = largest value <= lookup (assumes ascending); -1 = smallest value >= lookup
        // (assumes descending); 0 = exact match with wildcards.
        var matchMode = matchType switch
        {
            > 0 => -1,
            < 0 => 1,
            _ => 0
        };

        var index = RangeSearch.Find(array, lookup, matchMode, wildcards: matchType == 0, out var searchError);

        if (searchError is not null)
        {
            return searchError;
        }

        return index >= 0 ? CellData.FromNumber(index + 1) : CellData.FromError(CellError.NA);
    }
}
