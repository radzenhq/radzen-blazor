using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a store for cells in a data sheet that supports pagination.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="sheet"></param>
/// <param name="pageSize"></param>
public class DataCellStore<T>(DataSheet<T> sheet, int pageSize = 20) : CellStore(sheet) where T : class, new()
{
    private int page = -1;
    private readonly IReadOnlyList<IColumnMapping<T>> columnAccessors = sheet.ColumnMappings;

    private async Task FetchPageAsync(int pageNumber)
    {
        if (page != pageNumber)
        {
            page = pageNumber;

            var data = await sheet.DataLoader(new DataSheetLoaderRequest
            {
                Take = pageSize,
                Skip = pageNumber * pageSize
            });

            // Populate cells for the loaded page
            for (var i = 0; i < data.Count; i++)
            {
                var dataItem = data[i];
                var row = pageNumber * pageSize + i + 1; // +1 for header row

                for (var column = 0; column < columnAccessors.Count; column++)
                {
                    var value = columnAccessors[column].GetValue(dataItem);
                    this[row, column].Value = value;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets a cell at the specified row and column.
    /// </summary>
    /// <param name="column"> The column index of the cell.</param>
    /// <param name="row"> The row index of the cell.</param>
    /// <returns>The cell at the specified row and column.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the row or column index is out of bounds.</exception>
    /// <remarks>
    /// The row and column indices are zero-based, so the first cell is at (0, 0).
    /// The method ensures that the specified row and column are within the bounds of the sheet's dimensions.
    /// If the cell does not exist in the store, it creates a new cell with the specified row and column,
    /// initializes it with the sheet reference, and adds it to the store.
    /// For the header row (row 0), it uses the column header names from the sheet's column mappings.
    /// For data rows, it fetches the data asynchronously based on the current page size and the row index,
    /// adjusting for the header row.
    /// </remarks>

    public override Cell this[int row, int column]
    {
        get
        {
            EnsureWithinBounds(row, column);

            if (!data.TryGetValue((row, column), out var cell))
            {
                // For row 0, use the column header
                if (row == 0)
                {
                    if (column < sheet.ColumnMappings.Count)
                    {
                        this[row, column] = cell = new(Sheet, new CellRef(row, column))
                        {
                            Value = sheet.ColumnMappings[column].Name
                        };
                    }
                }
                // For data rows, load from current page
                else
                {
                    var dataRow = row - 1; // Adjust for header row
                    var pageNumber = dataRow / pageSize;

                    // Start loading the page if not already loading
                    _ = FetchPageAsync(pageNumber);
                }

                // If no data was loaded, create an empty cell
                if (cell == null)
                {
                    return base[row, column];
                }
            }

            return cell;
        }

        set => base[row, column] = value;
    }
}