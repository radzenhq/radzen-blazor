#nullable enable

namespace Radzen.Documents.Spreadsheet;

abstract class UnaryMathFunctionBase : FormulaFunction
{
    protected abstract CellData Compute(double number);

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var arg = arguments.GetSingle("number");

        if (arg is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (arg.IsError)
        {
            return arg;
        }

        if (!arg.TryCoerceToNumber(out var number, allowBooleans: true, nonNumericTextAsZero: false))
        {
            return CellData.FromError(CellError.Value);
        }

        return Compute(number);
    }
}
