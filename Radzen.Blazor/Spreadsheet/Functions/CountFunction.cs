#nullable enable

namespace Radzen.Documents.Spreadsheet;

class CountFunction : FormulaFunction
{
    public override string Name => "COUNT";
    public override bool CoerceLiteralBooleans => true;

    public override FunctionParameter[] Parameters =>
    [
        new ("value", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("value");

        if (values == null || values.Count == 0)
        {
            return CellData.FromNumber(0);
        }

        var count = 0d;

        foreach (var argument in values)
        {
            if (argument.IsError || argument.IsEmpty)
            {
                continue;
            }

            if (argument.Type == CellDataType.Number || argument.Type == CellDataType.Date)
            {
                count++;
            }
        }

        return CellData.FromNumber(count);
    }
}