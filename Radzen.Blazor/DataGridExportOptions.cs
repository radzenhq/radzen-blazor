using System;
using System.Threading;
using Radzen.Documents.Spreadsheet;

namespace Radzen;

/// <summary>
/// Specifies which rows a DataGrid export includes.
/// </summary>
public enum DataGridExportScope
{
    /// <summary>
    /// Exports the rows on the current page.
    /// </summary>
    CurrentPage,

    /// <summary>
    /// Exports all rows after filtering and sorting.
    /// </summary>
    All
}

/// <summary>
/// Configures a DataGrid export.
/// </summary>
public class DataGridExportOptions
{
    /// <summary>
    /// Gets or sets the rows included in the export.
    /// </summary>
    public DataGridExportScope Scope { get; set; } = DataGridExportScope.CurrentPage;

    /// <summary>
    /// Gets or sets the number of rows requested per LoadData call when exporting all rows.
    /// </summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the callback invoked after each LoadData chunk is exported.
    /// </summary>
    public Action<DataGridExportProgress>? Progress { get; set; }

    /// <summary>
    /// Gets or sets the cancellation token for the export.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets the export title. Excel and CSV use it as the worksheet name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets whether the export reads the current theme from the rendered grid and applies its header and row colors, gridlines and font size. Applies to Excel.
    /// </summary>
    public bool UseTheme { get; set; } = true;
}

/// <summary>
/// Reports progress while a DataGrid loads export rows in chunks.
/// </summary>
public class DataGridExportProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridExportProgress"/> class.
    /// </summary>
    /// <param name="loaded">The number of rows loaded so far.</param>
    /// <param name="total">The total number of rows, or zero when it is unknown.</param>
    public DataGridExportProgress(int loaded, int total)
    {
        Loaded = loaded;
        Total = total;
    }

    /// <summary>
    /// Gets the number of rows loaded so far.
    /// </summary>
    public int Loaded { get; }

    /// <summary>
    /// Gets the total number of rows, or zero when it is unknown.
    /// </summary>
    public int Total { get; }
}

/// <summary>
/// Configures a DataGrid Excel export.
/// </summary>
public class DataGridExcelExportOptions : DataGridExportOptions
{
}

/// <summary>
/// Configures a DataGrid CSV export.
/// </summary>
public class DataGridCsvExportOptions : DataGridExportOptions
{
    /// <summary>
    /// Gets or sets the CSV writer options.
    /// </summary>
    public CsvExportOptions CsvOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether non-numeric cell values starting with '=', '@', '+', or '-' are prefixed with an apostrophe so spreadsheet applications do not evaluate them as formulas.
    /// </summary>
    public bool EscapeFormulas { get; set; } = true;
}
