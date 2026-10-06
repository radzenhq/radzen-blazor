using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Documents.Spreadsheet;

internal sealed class LazyArgument
{
    private readonly Func<List<CellData>>? evaluate;
    private List<CellData>? values;

    public LazyArgument(Func<List<CellData>> evaluate)
    {
        this.evaluate = evaluate;
    }

    public LazyArgument(List<CellData> values)
    {
        this.values = values;
    }

    public List<CellData> Values => values ??= evaluate!();

    public CellData Value => Values.Count > 0 ? Values[0] : CellData.FromError(CellError.Value);
}
