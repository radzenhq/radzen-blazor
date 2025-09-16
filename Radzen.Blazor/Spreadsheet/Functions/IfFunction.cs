#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class IfFunction : FormulaFunction
{
    public override FunctionParameter[] Parameters =>
    [
        new("condition", ParameterType.Single, isRequired: true),
        new("true_value", ParameterType.Single, isRequired: true),
        new("false_value", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var condition = arguments.GetSingle("condition");

        if (condition == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (condition.IsError)
        {
            return condition;
        }

        var trueValue = arguments.GetSingle("true_value");

        if (trueValue == null)
        {
            return CellData.FromError(CellError.Value);
        }

        var falseValue = arguments.GetSingle("false_value") ?? CellData.FromBoolean(false);

        if (condition.IsEmpty)
        {
            return falseValue;
        }

        var value = condition.GetValueOrDefault<bool?>();

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