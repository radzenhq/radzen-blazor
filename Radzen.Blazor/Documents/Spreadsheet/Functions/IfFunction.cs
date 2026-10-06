#nullable enable

namespace Radzen.Documents.Spreadsheet;

class IfFunction : FormulaFunction
{
    public override string Name => "IF";

    public override bool CanHandleErrors => true;

    public override FunctionParameter[] Parameters =>
    [
        new("logical_test", ParameterType.Single, isRequired: true),
        new("value_if_true", ParameterType.Single, isRequired: true) { IsLazy = true },
        new("value_if_false", ParameterType.Single, isRequired: false) { IsLazy = true }
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var logicalTest = arguments.GetSingle("logical_test");

        if (logicalTest is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (logicalTest.IsError)
        {
            return logicalTest;
        }

        if (logicalTest.IsEmpty)
        {
            return arguments.GetSingle("value_if_false") ?? CellData.FromBoolean(false);
        }

        var value = logicalTest.GetValueOrDefault<bool?>();

        if (value is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (value.Value)
        {
            return arguments.GetSingle("value_if_true") ?? CellData.FromError(CellError.Value);
        }

        return arguments.GetSingle("value_if_false") ?? CellData.FromBoolean(false);
    }
}