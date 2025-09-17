#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class CountAllFunction : FormulaFunction
{
    public override string Name => "COUNTA";

    public override FunctionParameter[] Parameters => 
    [
        new ("value", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("value");

        if (values == null || values.Count == 0)
        {
            return CellData.FromNumber(0d);
        }

        var count = 0d;

        foreach (var v in values)
        {
            if (v.IsEmpty)
            {
                continue;
            }

            count++;
        }

        return CellData.FromNumber(count);
    }
}