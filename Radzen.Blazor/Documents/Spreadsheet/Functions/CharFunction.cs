#nullable enable

namespace Radzen.Documents.Spreadsheet;

class CharFunction : FormulaFunction
{
    public override string Name => "CHAR";

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetInteger(arguments, "number", isRequired: true, defaultValue: null, out var number, out var error))
        {
            return error!;
        }

        if (number < 1 || number > 255)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromString(Windows1252.ToChar(number).ToString());
    }
}
