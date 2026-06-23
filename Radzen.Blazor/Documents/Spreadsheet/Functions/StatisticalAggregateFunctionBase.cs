#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

// Collects numbers using Excel's AVERAGE rule: errors propagate, only true Number cells count
// (numeric text from cell references is ignored), and literal boolean constants coerce to 0/1.
abstract class StatisticalAggregateFunctionBase : FormulaFunction
{
    public override bool CoerceLiteralBooleans => true;

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Sequence, isRequired: true)
    ];

    protected abstract CellData Compute(List<double> numbers);

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("number");

        if (values is null || values.Count == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        var numbers = new List<double>();

        foreach (var argument in values)
        {
            if (argument.IsError)
            {
                return argument;
            }

            if (argument.IsEmpty || argument.Type != CellDataType.Number)
            {
                continue;
            }

            numbers.Add(argument.GetValueOrDefault<double>());
        }

        return Compute(numbers);
    }
}
