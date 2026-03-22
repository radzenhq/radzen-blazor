#nullable enable

namespace Radzen.Documents.Spreadsheet;

class VerticalLookupFunction : LookupFunctionBase
{
    public override string Name => "VLOOKUP";

    protected override (int searchCount, int resultCount) GetSearchAndResultCounts(int rows, int columns) => (rows, columns);

    protected override (int rows, int columns) GetFallbackDimensions(int count) => (count, 1);

    protected override int GetSearchCellIndex(int i, int columns) => i * columns;

    protected override int GetResultCellIndex(int matchPosition, int requestedIndex, int columns) => matchPosition * columns + (requestedIndex - 1);
}
