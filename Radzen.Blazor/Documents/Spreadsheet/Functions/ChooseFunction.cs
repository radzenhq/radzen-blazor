#nullable enable

namespace Radzen.Documents.Spreadsheet;

class ChooseFunction : FormulaFunction
{
    public override string Name => "CHOOSE";

    public override FunctionParameter[] Parameters =>
    [
        new("index_num", ParameterType.Single, isRequired: true),
        // Group (not Sequence) so each value argument stays selectable; flattening would let the index
        // pick a cell out of a range instead of the whole argument.
        new("value", ParameterType.Group, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var groups = arguments.GetGroups("value");

        if (groups is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (!TryGetInteger(arguments, "index_num", isRequired: true, defaultValue: null, out var idx, out var error))
        {
            return error!;
        }

        if (idx < 1 || idx > groups.Count)
        {
            return CellData.FromError(CellError.Value);
        }

        // Engine has no array spill; a multi-cell selected argument yields its first cell.
        var selected = groups[idx - 1];

        return selected.Count > 0 ? selected[0] : CellData.FromError(CellError.Value);
    }
}