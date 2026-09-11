using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen.Documents.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a workbook in a spreadsheet.
/// </summary>
public class Workbook
{
    private readonly List<Worksheet> sheets = [];

    /// <summary>
    /// Gets the collection of sheets in the workbook.
    /// </summary>
    public IReadOnlyList<Worksheet> Sheets => sheets;

    private CultureInfo? culture;

    /// <summary>
    /// Gets or sets the culture used to parse typed cell input and to render values and number formats.
    /// If not set, falls back to <see cref="CultureInfo.CurrentCulture"/>.
    /// File storage (XLSX, CSV) and formula storage always use the invariant culture regardless of this setting.
    /// </summary>
    public CultureInfo Culture
    {
        get => culture ?? CultureInfo.CurrentCulture;
        set => culture = value;
    }

    /// <summary>
    /// Gets or sets the workbook protection settings.
    /// </summary>
    public WorkbookProtection Protection { get; set; } = new();

    /// <summary>
    /// Gets the workbook-scoped defined names. The key is the name and the value is the reference or
    /// expression it refers to, written as a formula without the leading equals sign, for example
    /// <c>Sheet1!$A$1:$A$10</c>. Formulas resolve names case-insensitively.
    /// </summary>
    public IDictionary<string, string> DefinedNames { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, FormulaSyntaxTree> definedNameTrees = [];

    internal FormulaSyntaxTree? ResolveDefinedName(string name)
    {
        if (!DefinedNames.TryGetValue(name, out var refersTo))
        {
            return null;
        }

        if (!definedNameTrees.TryGetValue(refersTo, out var tree))
        {
            tree = FormulaParser.Parse("=" + refersTo);
            definedNameTrees[refersTo] = tree;
        }

        return tree;
    }

    internal Workbook(Worksheet sheet)
    {
        AddSheet(sheet);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Workbook"/> class.
    /// </summary>
    public Workbook()
    {
    }

    /// <summary>
    /// Adds a new sheet to the workbook with the specified name, rows, and columns.
    /// </summary>
    public Worksheet AddSheet(string name, int rows, int columns)
    {
        var sheet = new Worksheet(rows, columns)
        {
            Name = name
        };
        AddSheet(sheet);
        return sheet;
    }

    /// <summary>
    /// Adds an existing sheet to the workbook.
    /// </summary>
    /// <param name="sheet"></param>
    public void AddSheet(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        sheets.Add(sheet);
        sheet.Workbook = this;
    }

    /// <summary>
    /// Gets the sheet with the specified name or null if not found.
    /// </summary>
    /// <param name="name"></param>
    public Worksheet? GetSheet(string name)
    {
        foreach (var sheet in sheets)
        {
            if (string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return sheet;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes a sheet from the workbook.
    /// </summary>
    /// <param name="sheet">The sheet to remove.</param>
    /// <returns><c>true</c> if the sheet was removed; otherwise <c>false</c>.</returns>
    public bool RemoveSheet(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        return sheets.Remove(sheet);
    }

    /// <summary>
    /// Gets the index of the specified sheet in the workbook.
    /// </summary>
    /// <param name="sheet">The sheet to find.</param>
    /// <returns>The zero-based index of the sheet, or -1 if not found.</returns>
    public int IndexOf(Worksheet sheet)
    {
        return sheets.IndexOf(sheet);
    }

    /// <summary>
    /// Moves a sheet from one position to another.
    /// </summary>
    /// <param name="fromIndex">The current index of the sheet.</param>
    /// <param name="toIndex">The target index for the sheet.</param>
    public void MoveSheet(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
        }

        if (toIndex < 0 || toIndex >= sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        }

        var sheet = sheets[fromIndex];
        sheets.RemoveAt(fromIndex);
        sheets.Insert(toIndex, sheet);
    }

    /// <summary>
    /// Saves the workbook to the specified stream in the Open XML Spreadsheet format (XLSX).
    /// </summary>
    /// <param name="stream"></param>
    public void SaveToStream(Stream stream)
    {
        new XlsxWriter(this).Write(stream);
    }

    /// <summary>
    /// Saves the workbook to the specified stream in the Open XML Spreadsheet format (XLSX), appending
    /// rows read from a source to its only sheet without building a cell for any of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The workbook is the frame: its header, widths, frozen panes and tables are written as they are,
    /// and the rows start directly below the last row it holds. They are read once, in order, and none
    /// is kept, so what the save holds does not grow with the source: the appended rows' text is written
    /// in each cell rather than in the shared string table, which keeps the frame's text only. A table whose last row is the
    /// frame's last row, and which has no totals row, is written to cover the appended rows; the
    /// workbook's own table is not changed. The <c>dimension</c> element is left out, which readers
    /// accept.
    /// </para>
    /// <para>
    /// Each row is one <see cref="CellData"/> per column, and a null writes nothing. Use
    /// <see cref="CellData.FromString"/> to keep text that looks like a number as text: it is written
    /// with the quote prefix, as <see cref="Cell.SetValue"/> writes text given a leading apostrophe.
    /// </para>
    /// <para>
    /// A source that faults, a cancellation, or a limit breached leaves nothing that opens: a seekable
    /// stream is truncated back to where it started.
    /// </para>
    /// </remarks>
    /// <param name="stream">Destination stream. Not closed by this method.</param>
    /// <param name="rows">The rows to append.</param>
    /// <param name="cancellationToken">Cancels the save between rows.</param>
    /// <exception cref="InvalidOperationException">
    /// The workbook has more than one sheet, the rows run past the 1,048,576 a worksheet holds, a row
    /// has more than 16,384 cells, or a string is longer than the 32,767 characters a cell holds.
    /// </exception>
    public Task SaveToStreamAsync(Stream stream, IAsyncEnumerable<CellData?[]> rows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(rows);

        return new XlsxWriter(this).WriteAsync(stream, XlsxWriter.WithoutFormats(rows, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Loads a workbook from the specified stream in the Open XML Spreadsheet format (XLSX).
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static Workbook LoadFromStream(Stream stream)
    {
        return XlsxReader.Read(stream);
    }

    /// <summary>
    /// Saves a single sheet of the workbook to the specified stream in CSV format. By default
    /// the first sheet is exported; pass <see cref="CsvExportOptions.Sheet"/> to choose a
    /// different one. CSV is single-sheet by design, matching Excel's "Save As CSV" behavior.
    /// </summary>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">CSV options. When null, defaults are used (comma separator, UTF-8 with BOM, CRLF line endings, RFC 4180 minimal quoting).</param>
    public void SaveAsCsv(Stream stream, CsvExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        options ??= new CsvExportOptions();

        var sheet = options.Sheet
            ?? (sheets.Count > 0 ? sheets[0] : throw new InvalidOperationException("Workbook contains no sheets."));

        new CsvWriter(sheet, options).Write(stream);
    }

    /// <summary>
    /// Loads a CSV stream into a new <see cref="Workbook"/> with a single sheet.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="options">CSV options. When null, defaults are used.</param>
    public static Workbook LoadFromCsv(Stream stream, CsvImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return CsvReader.Read(stream, options ?? new CsvImportOptions());
    }
}
