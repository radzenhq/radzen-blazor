using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class ErrorFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments) => CellData.FromError(CellError.Name);
}
