#nullable enable

using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

abstract class KOrderFunctionBase : FormulaFunction
{
    protected abstract CellData Compute(List<double> numbers, int k);

    public override FunctionParameter[] Parameters =>
    [
        new("array", ParameterType.Collection, isRequired: true),
        new("k", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var arrayArg = arguments.GetRange("array");
        var kArg = arguments.GetSingle("k");

        if (arrayArg == null || kArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (kArg.IsError)
        {
            return kArg;
        }

        var k = kArg.GetValueOrDefault<int?>();

        if (k is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var numbers = new List<double>();

        foreach (var cell in arrayArg)
        {
            if (cell.IsError)
            {
                return cell;
            }

            if (cell.Type == CellDataType.Number)
            {
                numbers.Add(cell.GetValueOrDefault<double>());
            }
        }

        if (numbers.Count == 0)
        {
            return CellData.FromError(CellError.Num);
        }

        if (k.Value <= 0 || k.Value > numbers.Count)
        {
            return CellData.FromError(CellError.Num);
        }

        return Compute(numbers, k.Value);
    }
}