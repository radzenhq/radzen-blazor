#nullable enable

namespace Radzen.Documents.Spreadsheet;

class IfNaFunction : FormulaFunction
{
    public override string Name => "IFNA";

    public override bool CanHandleErrors => true;

    public override FunctionParameter[] Parameters =>
    [
        new("value", ParameterType.Single, isRequired: true),
        new("value_if_na", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var value = arguments.GetSingle("value");
        var valueIfNa = arguments.GetSingle("value_if_na");

        if (value is null || valueIfNa is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (value.IsError && value.GetValueOrDefault<CellError>() == CellError.NA)
        {
            return valueIfNa.IsEmpty ? CellData.FromString("") : valueIfNa;
        }

        return value.IsEmpty ? CellData.FromString("") : value;
    }
}
