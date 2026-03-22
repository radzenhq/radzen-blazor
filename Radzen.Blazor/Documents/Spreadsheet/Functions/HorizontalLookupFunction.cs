#nullable enable

namespace Radzen.Documents.Spreadsheet;

class HorizontalLookupFunction : LookupFunctionBase
{
    public override string Name => "HLOOKUP";

    protected override (int searchCount, int resultCount) GetSearchAndResultCounts(int rows, int columns) => (columns, rows);

    protected override (int rows, int columns) GetFallbackDimensions(int count) => (1, count);

    protected override int GetSearchCellIndex(int i, int columns) => i;

    protected override int GetResultCellIndex(int matchPosition, int requestedIndex, int columns) => (requestedIndex - 1) * columns + matchPosition;
}
