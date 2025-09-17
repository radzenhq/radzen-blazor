#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class NotFunction : FormulaFunction
{
    public override string Name => "NOT";

    public override FunctionParameter[] Parameters =>
    [
        new("logical", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var value = arguments.GetSingle("logical");

        if (value == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (value.IsError)
        {
            return value;
        }

        if (value.IsEmpty)
        {
            return CellData.FromBoolean(true);
        }

        var boolValue = value.GetValueOrDefault<bool?>();

        if (boolValue is null)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromBoolean(!boolValue.Value);
    }
}