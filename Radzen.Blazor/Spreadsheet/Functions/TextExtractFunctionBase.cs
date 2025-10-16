#nullable enable

namespace Radzen.Blazor.Spreadsheet;

abstract class TextExtractFunctionBase : FormulaFunction
{
    protected static CellData Substring(string text, int start, int count)
    {
        if (count < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (count == 0)
        {
            return CellData.FromString(string.Empty);
        }

        if (start >= text.Length)
        {
            return CellData.FromString(string.Empty);
        }

        var available = text.Length - start;
        var take = count > available ? available : count;

        if (take == text.Length && start == 0)
        {
            return CellData.FromString(text);
        }

        return CellData.FromString(text.Substring(start, take));
    }
}