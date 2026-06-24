#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class ExactFunction : FormulaFunction
{
    public override string Name => "EXACT";

    public override FunctionParameter[] Parameters =>
    [
        new("text1", ParameterType.Single, isRequired: true),
        new("text2", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var text1 = arguments.GetSingle("text1");
        var text2 = arguments.GetSingle("text2");

        if (text1 is null || text2 is null)
        {
            return CellData.FromError(CellError.Value);
        }

        // EXACT is case-sensitive, unlike the rest of the engine's text comparisons.
        return CellData.FromBoolean(string.Equals(text1.ToString(), text2.ToString(), StringComparison.Ordinal));
    }
}
