using System;
using System.Threading;
using Radzen.Documents;
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
    /// Gets or sets the export title. PDF uses it as the document title and heading; Excel and CSV use it as the worksheet name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets whether the export reads the current theme from the rendered grid and applies its header background and foreground colors and row and alternating row background colors. Applies to PDF and Excel.
    /// </summary>
    public bool UseThemeColors { get; set; } = true;
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

/// <summary>
/// Configures a DataGrid PDF export.
/// </summary>
public class DataGridPdfExportOptions : DataGridExportOptions
{
    /// <summary>
    /// Gets or sets the PDF page size.
    /// </summary>
    public PageSize PageSize { get; set; } = PageSizes.A4;

    /// <summary>
    /// Gets or sets the PDF page orientation.
    /// </summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Landscape;

    /// <summary>
    /// Gets or sets the page margin in points.
    /// </summary>
    public double Margin { get; set; } = 40;

    /// <summary>
    /// Gets or sets whether the table header repeats on every page.
    /// </summary>
    public bool RepeatHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the footer displays page numbers.
    /// </summary>
    public bool ShowPageNumbers { get; set; } = true;

    /// <summary>
    /// Gets or sets the table font family. A null value uses the document engine default.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the table font size in points.
    /// </summary>
    public double FontSize { get; set; } = 9;
}
