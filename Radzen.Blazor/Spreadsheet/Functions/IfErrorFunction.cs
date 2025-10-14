#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class IfErrorFunction : FormulaFunction
{
    public override string Name => "IFERROR";

    public override bool CanHandleErrors => true;

    public override FunctionParameter[] Parameters =>
    [
        new("value", ParameterType.Single, isRequired: true),
        new("value_if_error", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var value = arguments.GetSingle("value");

        if (value == null)
        {
            return CellData.FromError(CellError.Value);
        }

        var valueIfError = arguments.GetSingle("value_if_error");

        if (valueIfError == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (value.IsError)
        {
            return valueIfError.IsEmpty ? CellData.FromString("") : valueIfError;
        }

        return value.IsEmpty ? CellData.FromString("") : value;
    }
}