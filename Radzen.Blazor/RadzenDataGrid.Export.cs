using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Radzen.Documents;
using Radzen.Documents.Core;
using Radzen.Documents.Pdf;
using Radzen.Documents.Spreadsheet;
using DocumentColor = Radzen.Documents.Core.Color;
using DocumentHorizontalAlignment = Radzen.Documents.HorizontalAlignment;
using DocumentTable = Radzen.Documents.Table;
using SpreadsheetBorder = Radzen.Documents.Spreadsheet.BorderStyle;
using SpreadsheetFormat = Radzen.Documents.Spreadsheet.Format;

namespace Radzen.Blazor
{
    public partial class RadzenDataGrid<TItem> where TItem : notnull
    {
        private bool exporting;

        /// <summary>
        /// Exports the DataGrid to a PDF file and downloads it in the browser.
        /// </summary>
        /// <param name="fileName">The downloaded file name.</param>
        /// <param name="options">The PDF export options.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExportToPdfAsync(string fileName = "export.pdf", DataGridPdfExportOptions? options = null)
        {
            var document = await ToPdfDocumentAsync(options);
            using var stream = new MemoryStream();
            await document.SaveAsPdfAsync(stream);
            await DownloadAsync(fileName, stream, "application/pdf");
        }

        /// <summary>
        /// Exports the DataGrid to an Excel workbook and downloads it in the browser.
        /// </summary>
        /// <param name="fileName">The downloaded file name.</param>
        /// <param name="options">The export options.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExportToExcelAsync(string fileName = "export.xlsx", DataGridExportOptions? options = null)
        {
            var workbook = await ToWorkbookAsync(options);
            using var stream = new MemoryStream();
            workbook.SaveToStream(stream);
            await DownloadAsync(fileName, stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        /// <summary>
        /// Exports the DataGrid to a CSV file and downloads it in the browser.
        /// </summary>
        /// <param name="fileName">The downloaded file name.</param>
        /// <param name="options">The CSV export options.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExportToCsvAsync(string fileName = "export.csv", DataGridCsvExportOptions? options = null)
        {
            options ??= new DataGridCsvExportOptions();
            var workbook = await ToCsvWorkbookAsync(options);
            using var stream = new MemoryStream();
            workbook.SaveAsCsv(stream, options.CsvOptions);
            await DownloadAsync(fileName, stream, "text/csv");
        }

        private async Task DownloadAsync(string fileName, MemoryStream stream, string mimeType)
        {
            stream.Position = 0;
            using var streamReference = new DotNetStreamReference(stream);
            await JSRuntime!.InvokeVoidAsync("Radzen.downloadFile", fileName, streamReference, mimeType);
        }

        /// <summary>
        /// Creates a PDF document containing the exported DataGrid rows.
        /// </summary>
        /// <param name="options">The PDF export options.</param>
        /// <returns>The PDF authoring document.</returns>
        public async Task<Document> ToPdfDocumentAsync(DataGridPdfExportOptions? options = null)
        {
            options ??= new DataGridPdfExportOptions();
            ValidateOptions(options);

            var columns = GetExportColumns();
            var document = new Document();
            var section = document.Sections.Add();
            section.PageSize = options.PageSize;
            section.Orientation = options.Orientation;
            section.Margins.SetAll(Unit.FromPoint(options.Margin));

            if (!string.IsNullOrEmpty(options.Title))
            {
                document.Info.Title = options.Title;
                var title = section.Blocks.Add(new Paragraph(options.Title));
                title.Font.Bold = true;
                title.Font.Size = Unit.FromPoint(options.FontSize * 1.5);
                title.SpacingAfter = Unit.FromPoint(options.FontSize);
                if (!string.IsNullOrEmpty(options.FontFamily))
                {
                    title.Font.Family = options.FontFamily;
                }
            }

            if (options.ShowPageNumbers)
            {
                var footer = section.Footer.Blocks.Add(new Paragraph());
                footer.Alignment = DocumentHorizontalAlignment.Right;
                footer.Font.Size = Unit.FromPoint(Math.Max(6, options.FontSize - 1));
                if (!string.IsNullOrEmpty(options.FontFamily))
                {
                    footer.Font.Family = options.FontFamily;
                }
                footer.Inlines.Add("Page ");
                footer.Inlines.Add(new PageNumberField());
                footer.Inlines.Add(" of ");
                footer.Inlines.Add(new PageCountField());
            }

            var table = section.Blocks.Add(new DocumentTable());
            table.Font.Size = Unit.FromPoint(options.FontSize);
            if (!string.IsNullOrEmpty(options.FontFamily))
            {
                table.Font.Family = options.FontFamily;
            }

            foreach (var gridColumn in columns)
            {
                var column = table.Columns.Add();
                if (TryGetPointWidth(gridColumn.Width, out var width))
                {
                    column.Width = Unit.FromPoint(width);
                }
                else
                {
                    column.RelativeWidth = 1;
                }
                column.Alignment = GetDocumentAlignment(gridColumn);
            }

            var theme = await GetExportThemeAsync(options);
            var gridLines = ResolveGridLines(theme);

            var header = table.Rows.Add();
            header.IsHeaderRow = true;
            header.RepeatOnEveryPage = options.RepeatHeader;
            header.Font.Bold = true;
            header.Background = ParseCssColor(theme?.HeaderBackground) ?? DocumentColor.FromRgb(242, 244, 247);
            header.Font.Color = ParseCssColor(theme?.HeaderColor);
            SetBorder(header.Borders.Bottom, 0.75, DocumentColor.FromRgb(148, 155, 164));
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                header.Cells[columnIndex].Text = columns[columnIndex].GetTitle();
                header.Cells[columnIndex].Padding = Unit.FromPoint(4);
                ApplyGridLines(header.Cells[columnIndex], true, gridLines);
            }

            var rowBackground = ParseCssColor(theme?.RowBackground);
            var alternatingRowBackground = ParseCssColor(theme?.AlternatingRowBackground);
            var rowColor = ParseCssColor(theme?.RowColor);
            var rowIndex = 0;
            await foreach (var item in GetExportRows(options, options.CancellationToken))
            {
                var row = table.Rows.Add();
                row.KeepTogether = true;
                row.Background = rowIndex % 2 == 0 ? rowBackground : alternatingRowBackground;
                row.Font.Color = rowColor;
                for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                {
                    var cell = row.Cells[columnIndex];
                    cell.Text = Convert.ToString(columns[columnIndex].GetValue(item), Culture) ?? string.Empty;
                    cell.Padding = Unit.FromPoint(4);
                    ApplyGridLines(cell, false, gridLines);
                }
                rowIndex++;
            }

            return document;
        }

        /// <summary>
        /// Creates an Excel workbook containing the exported DataGrid rows.
        /// </summary>
        /// <param name="options">The export options.</param>
        /// <returns>The workbook.</returns>
        public Task<Workbook> ToWorkbookAsync(DataGridExportOptions? options = null)
        {
            return CreateWorkbookAsync(options ?? new DataGridExportOptions(), false);
        }

        internal Task<Workbook> ToCsvWorkbookAsync(DataGridCsvExportOptions options)
        {
            return CreateWorkbookAsync(options, true);
        }

        /// <inheritdoc />
        protected override bool ShouldRender()
        {
            return !exporting && base.ShouldRender();
        }

        private async Task<Workbook> CreateWorkbookAsync(DataGridExportOptions options, bool formattedStrings)
        {
            ValidateOptions(options);
            var escapeFormulas = (options as DataGridCsvExportOptions)?.EscapeFormulas == true;
            var columns = GetExportColumns();
            var theme = formattedStrings ? null : await GetExportThemeAsync(options);
            var headerBackground = ToHexColor(theme?.HeaderBackground);
            var headerColor = ToHexColor(theme?.HeaderColor);
            var rowBackground = ToHexColor(theme?.RowBackground);
            var alternatingRowBackground = ToHexColor(theme?.AlternatingRowBackground);
            var rowColor = ToHexColor(theme?.RowColor);
            var gridLines = ResolveGridLines(theme);
            var horizontalBorder = theme != null && gridLines.Horizontal ? new SpreadsheetBorder { Color = ToHexColor(theme.HorizontalBorder) ?? "#D3D7DC" } : null;
            var verticalBorder = theme != null && gridLines.Vertical ? new SpreadsheetBorder { Color = ToHexColor(theme.VerticalBorder) ?? "#D3D7DC" } : null;
            var workbook = new Workbook { Culture = Culture };
            var sheet = workbook.AddSheet(GetSheetName(options.Title), 1, columns.Count);
            sheet.BeginUpdate();
            try
            {
                for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                {
                    var cell = sheet.Cells[0, columnIndex];
                    SetStringValue(cell, columns[columnIndex].GetTitle());
                    cell.Format = new SpreadsheetFormat { Bold = true, BackgroundColor = headerBackground, Color = headerColor };
                    ApplyExcelBorders(cell, horizontalBorder, verticalBorder);
                }

                var rowIndex = 1;
                await foreach (var item in GetExportRows(options, options.CancellationToken))
                {
                    sheet.Rows.Count = rowIndex + 1;
                    var background = rowIndex % 2 == 1 ? rowBackground : alternatingRowBackground;
                    for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                    {
                        var column = columns[columnIndex];
                        var cell = sheet.Cells[rowIndex, columnIndex];
                        if (formattedStrings)
                        {
                            var text = Convert.ToString(column.GetValue(item), Culture) ?? string.Empty;
                            SetStringValue(cell, escapeFormulas && IsFormulaLike(text) ? "'" + text : text);
                        }
                        else
                        {
                            SetWorkbookValue(cell, column, item);
                            cell.Format.BackgroundColor = background;
                            cell.Format.Color = rowColor;
                            ApplyExcelBorders(cell, horizontalBorder, verticalBorder);
                        }
                    }
                    rowIndex++;
                }
            }
            finally
            {
                sheet.EndUpdate();
            }
            return workbook;
        }

        private List<RadzenDataGridColumn<TItem>> GetExportColumns()
        {
            return FlattenColumns(ColumnsCollection).Where(column => !string.IsNullOrEmpty(column.Property) || column.ExportValue != null).ToList();
        }

        private static IEnumerable<RadzenDataGridColumn<TItem>> FlattenColumns(IEnumerable<RadzenDataGridColumn<TItem>> columns)
        {
            foreach (var column in columns.Where(column => column.GetVisible()))
            {
                if (column.ColumnsCollection.Count > 0)
                {
                    foreach (var child in FlattenColumns(column.ColumnsCollection))
                    {
                        yield return child;
                    }
                }
                else
                {
                    yield return column;
                }
            }
        }

        private async IAsyncEnumerable<TItem> GetExportRows(DataGridExportOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!LoadData.HasDelegate || options.Scope == DataGridExportScope.CurrentPage)
            {
                var rows = options.Scope == DataGridExportScope.CurrentPage ? PagedView : View;
                foreach (var item in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }
                yield break;
            }

            if (exporting)
            {
                throw new InvalidOperationException("A DataGrid export is already in progress.");
            }

            var wasLoading = IsLoading;
            var restoreStart = lastLoadDataArgs != null ? lastLoadDataStart : skip;
            var restoreTop = lastLoadDataArgs != null && lastLoadDataTop > 0 ? lastLoadDataTop : PageSize;
            IsLoading = true;
            await InvokeAsync(StateHasChanged);
            exporting = true;
            try
            {
                var loaded = 0;
                object? previousFirst = null;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await InvokeLoadData(loaded, options.ChunkSize, true);
                    IsLoading = true;
                    var chunk = View.ToList();
                    if (chunk.Count == 0 || loaded > 0 && Equals(chunk[0], previousFirst))
                    {
                        break;
                    }

                    previousFirst = chunk[0];
                    foreach (var item in chunk)
                    {
                        yield return item;
                    }

                    loaded += chunk.Count;
                    options.Progress?.Invoke(new DataGridExportProgress(loaded, Count));
                    if (Count > 0 ? loaded >= Count : chunk.Count < options.ChunkSize)
                    {
                        break;
                    }
                }
            }
            finally
            {
                try
                {
                    await InvokeLoadData(restoreStart, restoreTop, true);
                }
                finally
                {
                    exporting = false;
                    IsLoading = wasLoading;
                    OnDataChanged();
                    if (virtualize != null)
                    {
                        await virtualize.RefreshDataAsync();
                    }
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private void SetWorkbookValue(Radzen.Documents.Spreadsheet.Cell cell, RadzenDataGridColumn<TItem> column, TItem item)
        {
            var rawValue = column.GetRawValue(item);
            var valueType = Nullable.GetUnderlyingType(column.FilterPropertyType ?? rawValue?.GetType() ?? typeof(object)) ?? column.FilterPropertyType ?? rawValue?.GetType();
            if (rawValue is Enum || valueType?.IsEnum == true)
            {
                SetStringValue(cell, Convert.ToString(column.GetValue(item), Culture) ?? string.Empty);
                return;
            }

            if (TryGetNumberFormat(column.FormatString, rawValue, out var numberFormat) && IsSpreadsheetScalar(rawValue))
            {
                SetNativeValue(cell, rawValue);
                cell.Format.NumberFormat = numberFormat;
                return;
            }

            if (string.IsNullOrEmpty(column.FormatString) && IsSpreadsheetScalar(rawValue))
            {
                SetNativeValue(cell, rawValue);
                return;
            }

            SetStringValue(cell, Convert.ToString(column.GetValue(item), Culture) ?? string.Empty);
        }

        private static void ApplyExcelBorders(Radzen.Documents.Spreadsheet.Cell cell, SpreadsheetBorder? horizontal, SpreadsheetBorder? vertical)
        {
            if (horizontal != null)
            {
                cell.Format.BorderTop = horizontal.Clone();
                cell.Format.BorderBottom = horizontal.Clone();
            }
            if (vertical != null)
            {
                cell.Format.BorderLeft = vertical.Clone();
                cell.Format.BorderRight = vertical.Clone();
            }
        }

        private static void SetNativeValue(Radzen.Documents.Spreadsheet.Cell cell, object? value)
        {
            if (value is string text)
            {
                SetStringValue(cell, text);
            }
            else
            {
                cell.Value = value;
            }
        }

        private static void SetStringValue(Radzen.Documents.Spreadsheet.Cell cell, string value)
        {
            cell.Data = CellData.FromString(value);
        }

        private bool TryGetNumberFormat(string formatString, object? value, out string numberFormat)
        {
            numberFormat = string.Empty;
            if (string.IsNullOrEmpty(formatString) || !formatString.StartsWith("{0:", StringComparison.Ordinal) || !formatString.EndsWith('}'))
            {
                return false;
            }

            var specifier = formatString[3..^1];
            if (value is DateTime)
            {
                numberFormat = specifier switch
                {
                    "d" => Culture.DateTimeFormat.ShortDatePattern,
                    "D" => Culture.DateTimeFormat.LongDatePattern,
                    _ => string.Empty
                };
                return numberFormat.Length > 0;
            }

            if (specifier.Length == 0 || !IsNumericType(value?.GetType()))
            {
                return false;
            }

            var decimalPlaces = specifier[0] switch
            {
                'C' or 'c' => Culture.NumberFormat.CurrencyDecimalDigits,
                'P' or 'p' => Culture.NumberFormat.PercentDecimalDigits,
                _ => Culture.NumberFormat.NumberDecimalDigits
            };
            if (specifier.Length > 1 && !int.TryParse(specifier[1..], NumberStyles.None, CultureInfo.InvariantCulture, out decimalPlaces))
            {
                return false;
            }
            var decimals = decimalPlaces > 0 ? "." + new string('0', decimalPlaces) : string.Empty;
            numberFormat = specifier[0] switch
            {
                'C' or 'c' => GetCurrencyNumberFormat(decimals),
                'N' or 'n' => "#,##0" + decimals,
                'F' or 'f' => "0" + decimals,
                'P' or 'p' => "0" + decimals + "%",
                _ => string.Empty
            };
            return numberFormat.Length > 0;
        }

        private string GetCurrencyNumberFormat(string decimals)
        {
            var symbol = "\"" + Culture.NumberFormat.CurrencySymbol + "\"";
            var number = "#,##0" + decimals;
            return Culture.NumberFormat.CurrencyPositivePattern switch
            {
                0 => symbol + number,
                1 => number + symbol,
                2 => symbol + " " + number,
                _ => number + " " + symbol
            };
        }

        private async Task<DataGridExportTheme?> GetExportThemeAsync(DataGridExportOptions options)
        {
            if (!options.UseThemeColors || JSRuntime == null)
            {
                return null;
            }

            var rowBackgroundVariable = AllowAlternatingRows ? "--rz-grid-stripe-odd-background-color" : "--rz-grid-background-color";
            var alternatingRowBackgroundVariable = AllowAlternatingRows ? "--rz-grid-stripe-background-color" : "--rz-grid-background-color";
            try
            {
                var values = await JSRuntime.InvokeAsync<string?[]?>("Radzen.cssVariables", Element, new[]
                {
                    "--rz-grid-header-background-color",
                    "--rz-grid-header-color",
                    rowBackgroundVariable,
                    "--rz-grid-cell-color",
                    alternatingRowBackgroundVariable,
                    "--rz-grid-bottom-cell-border",
                    "--rz-grid-right-cell-border"
                });

                if (values == null || values.Length < 7)
                {
                    return null;
                }

                return new DataGridExportTheme
                {
                    HeaderBackground = values[0],
                    HeaderColor = values[1],
                    RowBackground = values[2],
                    RowColor = values[3],
                    AlternatingRowBackground = values[4],
                    HorizontalBorder = values[5],
                    VerticalBorder = values[6]
                };
            }
            catch (Exception exception) when (exception is JSException or InvalidOperationException or ArgumentException or JSDisconnectedException or TaskCanceledException)
            {
                return null;
            }
        }

        private static DocumentColor? ParseCssColor(string? value)
        {
            return value != null && ColorValue.Parse(value) is { Alpha: > 0 } color
                ? DocumentColor.FromRgb((byte)color.Red, (byte)color.Green, (byte)color.Blue)
                : null;
        }

        private static string? ToHexColor(string? value)
        {
            return value != null && ColorValue.Parse(value) is { Alpha: > 0 } color ? "#" + color.ToHex() : null;
        }

        private bool IsFormulaLike(string value)
        {
            return value.Length > 0 && value[0] is '=' or '@' or '+' or '-' && !double.TryParse(value, NumberStyles.Any, Culture, out _);
        }

        private static bool IsSpreadsheetScalar(object? value)
        {
            return value == null || value is string || value is bool || value is DateTime || IsNumericType(value.GetType());
        }

        private static bool IsNumericType(Type? type)
        {
            type = Nullable.GetUnderlyingType(type ?? typeof(object)) ?? type;
            return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
                type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        private static bool TryGetPointWidth(string width, out double points)
        {
            points = 0;
            return width.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
                double.TryParse(width[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels) &&
                (points = pixels * 72 / 96) > 0;
        }

        private static string GetSheetName(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "Export";
            }

            var name = new string(title.Where(character => character is not ':' and not '\\' and not '/' and not '?' and not '*' and not '[' and not ']').Take(31).ToArray()).Trim();
            return name.Length > 0 ? name : "Export";
        }

        private static DocumentHorizontalAlignment GetDocumentAlignment(RadzenDataGridColumn<TItem> column)
        {
            return column.TextAlign switch
            {
                TextAlign.Center => DocumentHorizontalAlignment.Center,
                TextAlign.Right or TextAlign.End => DocumentHorizontalAlignment.Right,
                _ => DocumentHorizontalAlignment.Left
            };
        }

        private readonly record struct ExportGridLines(bool Horizontal, bool Vertical, DocumentColor HorizontalColor, DocumentColor VerticalColor);

        private ExportGridLines ResolveGridLines(DataGridExportTheme? theme)
        {
            var fallback = DocumentColor.FromRgb(211, 215, 220);
            var (horizontal, vertical) = GridLines switch
            {
                DataGridGridLines.Both => (true, true),
                DataGridGridLines.Horizontal => (true, false),
                DataGridGridLines.Vertical => (false, true),
                DataGridGridLines.None => (false, false),
                _ => theme != null ? (HasBorder(theme.HorizontalBorder), HasBorder(theme.VerticalBorder)) : (true, false)
            };
            return new ExportGridLines(horizontal, vertical, ParseCssColor(theme?.HorizontalBorder) ?? fallback, ParseCssColor(theme?.VerticalBorder) ?? fallback);
        }

        private static bool HasBorder(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Contains("none", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var width = value.Split(' ')[0];
            if (width.EndsWith("px", StringComparison.OrdinalIgnoreCase) && double.TryParse(width[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels))
            {
                return pixels > 0;
            }

            return true;
        }

        private static void ApplyGridLines(Radzen.Documents.Cell cell, bool header, ExportGridLines gridLines)
        {
            if (gridLines.Horizontal)
            {
                SetBorder(cell.Borders.Bottom, 0.4, gridLines.HorizontalColor);
                if (!header)
                {
                    SetBorder(cell.Borders.Top, 0.4, gridLines.HorizontalColor);
                }
            }
            if (gridLines.Vertical)
            {
                SetBorder(cell.Borders.Left, 0.4, gridLines.VerticalColor);
                SetBorder(cell.Borders.Right, 0.4, gridLines.VerticalColor);
            }
        }

        private static void SetBorder(Border border, double width, DocumentColor color)
        {
            border.Width = Unit.FromPoint(width);
            border.Color = color;
            border.Style = Radzen.Documents.BorderStyle.Solid;
        }

        private static void ValidateOptions(DataGridExportOptions options)
        {
            if (options.ChunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "ChunkSize must be greater than zero.");
            }
        }
    }

    internal class DataGridExportTheme
    {
        public string? HeaderBackground { get; set; }

        public string? HeaderColor { get; set; }

        public string? RowBackground { get; set; }

        public string? RowColor { get; set; }

        public string? AlternatingRowBackground { get; set; }

        public string? HorizontalBorder { get; set; }

        public string? VerticalBorder { get; set; }
    }
}
