#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class IfFunction : FormulaFunction
{
    public override string Name => "IF";

    public override FunctionParameter[] Parameters =>
    [
        new("logical_test", ParameterType.Single, isRequired: true),
        new("value_if_true", ParameterType.Single, isRequired: true),
        new("value_if_false", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var logicalTest = arguments.GetSingle("logical_test");

        if (logicalTest == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (logicalTest.IsError)
        {
            return logicalTest;
        }

        var trueValue = arguments.GetSingle("value_if_true");

        if (trueValue == null)
        {
            return CellData.FromError(CellError.Value);
        }

        var falseValue = arguments.GetSingle("value_if_false") ?? CellData.FromBoolean(false);

        if (logicalTest.IsEmpty)
        {
            return falseValue;
        }

        var value = logicalTest.GetValueOrDefault<bool?>();

        if (value is null)
        {
            return CellData.FromError(CellError.Value);
        }
        else
        {
            return value.Value ? trueValue : falseValue;
        }
    }
}