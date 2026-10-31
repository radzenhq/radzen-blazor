using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

    internal List<ExternalLink> ExternalLinks { get; } = [];

    private readonly Dictionary<string, FormulaSyntaxTree> definedNameTrees = [];

    internal HashSet<string> HiddenDefinedNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal FormulaSyntaxTree? ResolveDefinedName(string name, Worksheet sheet)
    {
        if (!sheet.DefinedNames.TryGetValue(name, out var refersTo) && !DefinedNames.TryGetValue(name, out refersTo))
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

        if (sheets.Contains(sheet))
        {
            return;
        }

        if (sheet.CreatedWorkbook is { } previous && previous != this)
        {
            previous.Detach(sheet);
        }

        sheets.Add(sheet);
        sheet.Workbook = this;
        RefreshReferencesTo(sheet.Name, GetFormulaCells(sheet));
        RecalculateIfIdle();
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

        if (!sheets.Contains(sheet))
        {
            return false;
        }

        Detach(sheet);
        sheet.Workbook = new Workbook(sheet);

        return true;
    }

    private void Detach(Worksheet sheet)
    {
        if (!sheets.Remove(sheet))
        {
            return;
        }

        updatedSheets.Remove(sheet);

        foreach (var cell in GetFormulaCells(sheet))
        {
            OnFormulaCellRemoved(cell);
        }

        RefreshReferencesTo(sheet.Name, []);
        RecalculateIfIdle();
    }

    private void RecalculateIfIdle()
    {
        if (!IsUpdating)
        {
            Recalculate();
        }
    }

    internal CellDependencyGraph Graph { get; } = new();

    internal bool IsEvaluating { get; set; }

    private readonly HashSet<Worksheet> updatedSheets = [];

    private readonly HashSet<Cell> changedDuringUpdate = [];

    private readonly HashSet<Cell> pendingFormulaCells = [];

    private readonly HashSet<string> pendingSheetNames = new(StringComparer.OrdinalIgnoreCase);

    internal bool IsUpdating
    {
        get
        {
            foreach (var sheet in sheets)
            {
                if (sheet.IsUpdating)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal void OnSheetRenamed(Worksheet sheet, string oldName)
    {
        if (!sheets.Contains(sheet))
        {
            return;
        }

        var collides = sheets.Any(other => other != sheet && string.Equals(other.Name, sheet.Name, StringComparison.OrdinalIgnoreCase));

        sheet.Batch(() =>
        {
            if (!collides)
            {
                RenameSheetReferences(oldName, sheet.Name);
            }

            RefreshReferencesTo(oldName, []);
            RefreshReferencesTo(sheet.Name, []);
        });
    }

    internal void OnFormulaCellRemoved(Cell cell)
    {
        Graph.Remove(cell);
        pendingFormulaCells.Remove(cell);
    }

    private void RefreshReferencesTo(string sheetName, IEnumerable<Cell> cells)
    {
        pendingFormulaCells.UnionWith(cells);
        pendingSheetNames.Add(sheetName);
    }

    private void QueueFormulasReferencingPendingSheetNames()
    {
        if (pendingSheetNames.Count == 0)
        {
            return;
        }

        foreach (var cell in GetFormulaCells())
        {
            if (cell.FormulaSyntaxTree!.Find(node => node is NameSyntaxNode
                    || node is CellSyntaxNode c && c.Token.Address.Worksheet is { } name && pendingSheetNames.Contains(name)).Count > 0)
            {
                pendingFormulaCells.Add(cell);
            }
        }

        pendingSheetNames.Clear();
    }

    private List<Cell> GetFormulaCells() => [.. sheets.SelectMany(GetFormulaCells)];

    private static IEnumerable<Cell> GetFormulaCells(Worksheet sheet) =>
        sheet.Cells.GetPopulatedCells().Where(cell => cell.FormulaSyntaxTree is not null).ToList();

    private void RenameSheetReferences(string oldName, string newName)
    {
        foreach (var sheet in sheets)
        {
            foreach (var cell in GetFormulaCells(sheet))
            {
                var renamed = RenameSheetInFormula(cell.Formula!, cell.FormulaSyntaxTree!, oldName, newName);

                if (renamed is not null)
                {
                    cell.Formula = renamed;
                }
            }

            foreach (var series in sheet.Charts.SelectMany(chart => chart.Series))
            {
                series.Categories = RenameSheetInReference(series.Categories, oldName, newName);
                series.Values = RenameSheetInReference(series.Values, oldName, newName);
            }

            foreach (var rule in sheet.Validation.Ranges.SelectMany(sheet.Validation.GetValidators).OfType<DataValidationRule>())
            {
                rule.Formula1 = RenameSheetInReference(rule.Formula1, oldName, newName);
                rule.Formula2 = RenameSheetInReference(rule.Formula2, oldName, newName);
            }
        }

        foreach (var names in sheets.Select(sheet => sheet.DefinedNames).Prepend(DefinedNames))
        {
            foreach (var (name, refersTo) in names.ToList())
            {
                names[name] = RenameSheetInReference(refersTo, oldName, newName)!;
            }
        }
    }

    internal static string? AdjustReference(string? text, Worksheet owner, Worksheet target, AxisShift shift)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var hasEquals = text.StartsWith('=');
        var formula = hasEquals ? text : "=" + text;
        FormulaSyntaxTree tree;

        try
        {
            tree = FormulaParser.Parse(formula);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException)
        {
            return text;
        }

        if (tree.Errors.Count > 0)
        {
            return text;
        }

        var rewritten = ShiftReferences(formula, tree, owner, target, shift);

        if (string.Equals(rewritten, formula, StringComparison.Ordinal))
        {
            return text;
        }

        return hasEquals ? rewritten : rewritten[1..];
    }

    private static string? RenameSheetInReference(string? text, string oldName, string newName)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var hasEquals = text.StartsWith('=');
        var formula = hasEquals ? text : "=" + text;
        var renamed = RenameSheetInFormula(formula, FormulaParser.Parse(formula), oldName, newName);

        if (renamed is null)
        {
            return text;
        }

        return hasEquals ? renamed : renamed[1..];
    }

    private static string? RenameSheetInFormula(string formula, FormulaSyntaxTree tree, string oldName, string newName)
    {
        bool IsOldSheet(CellRef address) => string.Equals(address.Worksheet, oldName, StringComparison.OrdinalIgnoreCase);

        if (tree.Errors.Count > 0 || tree.Find(node => node is CellSyntaxNode or ErrorLiteralSyntaxNode && IsOldSheet(node.Token.Address)).Count == 0)
        {
            return null;
        }

        return FormulaRewriter.Rewrite(formula, tree,
            token => IsOldSheet(token.Address) ? token.Address with { Worksheet = newName } : token.Address);
    }

    internal void OnCellValueChanged(Cell cell)
    {
        if (!Graph.HasDependents(cell) && Graph.VolatileCells.Count == 0)
        {
            return;
        }

        if (IsUpdating)
        {
            changedDuringUpdate.Add(cell);
            return;
        }

        EvaluateFormulas(Graph.GetTopologicallySortedDependencies([cell, .. Graph.VolatileCells]));
    }

    internal void OnCellFormulaChanged(Cell cell)
    {
        if (IsUpdating)
        {
            pendingFormulaCells.Add(cell);
            return;
        }

        Graph.Add(cell);
        EvaluateFormulas(Graph.GetTopologicallySortedDependencies([cell, .. Graph.VolatileCells]));
    }

    internal void OnUpdateEnded(Worksheet sheet)
    {
        updatedSheets.Add(sheet);

        if (!IsUpdating)
        {
            Recalculate();
        }
    }

    private void Recalculate()
    {
        QueueFormulasReferencingPendingSheetNames();

        foreach (var cell in pendingFormulaCells)
        {
            Graph.Add(cell);
        }

        var roots = new List<Cell>(pendingFormulaCells);

        foreach (var cell in Graph.FormulaCells)
        {
            if (updatedSheets.Contains(cell.Worksheet))
            {
                roots.Add(cell);
            }
        }

        roots.AddRange(Graph.GetRangeDependents(updatedSheets));
        roots.AddRange(changedDuringUpdate);
        roots.AddRange(Graph.VolatileCells);

        pendingFormulaCells.Clear();
        updatedSheets.Clear();
        changedDuringUpdate.Clear();

        EvaluateFormulas(Graph.GetTopologicallySortedDependencies(roots));
    }

    private void EvaluateFormulas(IEnumerable<Cell> cells)
    {
        new Recalculation(cells).Run();
    }

    internal void AdjustFormulas(Worksheet target, AxisShift shift)
    {
        foreach (var cell in GetFormulaCells())
        {
            if (string.IsNullOrEmpty(cell.Formula) || cell.FormulaSyntaxTree!.Errors.Count > 0 || !References(cell, target))
            {
                continue;
            }

            var newFormula = ShiftReferences(cell.Formula, cell.FormulaSyntaxTree!, cell.Worksheet, target, shift);

            if (!string.Equals(newFormula, cell.Formula, StringComparison.Ordinal))
            {
                cell.Formula = newFormula;
            }
        }
    }

    private static string ShiftReferences(string formula, FormulaSyntaxTree tree, Worksheet owner, Worksheet target, AxisShift shift)
    {
        return FormulaRewriter.Rewrite(formula, tree,
            token => IsReferenceTo(target, owner, token.Address) ? shift.AdjustCell(token) : token.Address,
            (start, end, kind) => IsReferenceTo(target, owner, start.Address) ? shift.AdjustRange(start, end, kind) : (start.Address, end.Address));
    }

    private static bool IsReferenceTo(Worksheet target, Worksheet owner, CellRef address)
    {
        return string.IsNullOrEmpty(address.Worksheet)
            ? owner == target
            : string.Equals(address.Worksheet, target.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool References(Cell owner, Worksheet target)
    {
        return owner.Worksheet == target
            || owner.FormulaSyntaxTree!.Find(node => node is CellSyntaxNode c && IsReferenceTo(target, owner.Worksheet, c.Token.Address)).Count > 0;
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
    /// Saves the workbook as XLSX, appending source rows below the built rows in its only sheet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are read once and are not retained. Shared-string memory grows with distinct text.
    /// Tables ending at the last built row extend over appended rows unless they have a totals row.
    /// The workbook is not mutated.
    /// </para>
    /// <para>
    /// Each array entry represents one column; null entries write no cell.
    /// Use <see cref="CellData.FromString"/> to preserve numeric-looking text as text.
    /// </para>
    /// <para>
    /// On failure, seekable output is truncated to its starting position when supported.
    /// Discard any partial output on failure.
    /// </para>
    /// </remarks>
    /// <param name="stream">Destination stream. Not closed by this method.</param>
    /// <param name="rows">The rows to append.</param>
    /// <param name="cancellationToken">Cancels the save between rows.</param>
    /// <exception cref="InvalidOperationException">
    /// The workbook has more than one sheet, the rows run past the 1,048,576 a worksheet holds, a row
    /// has more than 16,384 cells, or a string is longer than the 32,767 characters a cell holds.
    /// </exception>
    public Task SaveToStreamAsync(Stream stream, IAsyncEnumerable<CellData?[]> rows, CancellationToken cancellationToken = default) =>
        SaveToStreamAsync(stream, rows, useInlineStrings: false, cancellationToken);

    /// <summary>
    /// Saves the workbook and appends rows to its only sheet, with a choice of text storage.
    /// </summary>
    /// <remarks>
    /// This has the same row, table and formatting behavior as
    /// <see cref="SaveToStreamAsync(Stream, IAsyncEnumerable{CellData?[]}, CancellationToken)"/>.
    /// Inline strings keep the appended text out of the shared string table, so the writer does not
    /// retain distinct strings from the source. Built cells still use shared strings. The source and
    /// destination may retain their own data; a <see cref="MemoryStream"/> retains the entire output.
    /// </remarks>
    /// <param name="stream">Destination stream. Not closed by this method.</param>
    /// <param name="rows">The rows to append.</param>
    /// <param name="useInlineStrings">Whether to store appended text in each cell instead of the shared string table.</param>
    /// <param name="cancellationToken">Cancels the save between rows.</param>
    public Task SaveToStreamAsync(Stream stream, IAsyncEnumerable<CellData?[]> rows, bool useInlineStrings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(rows);

        return new XlsxWriter(this).WriteAsync(stream, rows, useInlineStrings, cancellationToken);
    }

    /// <summary>
    /// Saves the workbook as XLSX, appending formatted rows to its only sheet.
    /// </summary>
    /// <remarks>
    /// Null formats use type defaults; blank values can carry formatting.
    /// Reuse formats without changing them or their borders during the save.
    /// </remarks>
    /// <param name="stream">Destination stream. Not closed by this method.</param>
    /// <param name="rows">The values and formats to append.</param>
    /// <param name="cancellationToken">Cancels the save between rows.</param>
    public Task SaveToStreamAsync(Stream stream, IAsyncEnumerable<(CellData? Data, Format? Format)[]> rows, CancellationToken cancellationToken = default) =>
        SaveToStreamAsync(stream, rows, useInlineStrings: false, cancellationToken);

    /// <summary>
    /// Saves the workbook as XLSX, appending formatted rows with optional inline strings.
    /// </summary>
    /// <remarks>
    /// Reuse formats without changing them or their borders during the save.
    /// </remarks>
    /// <param name="stream">Destination stream. Not closed by this method.</param>
    /// <param name="rows">The values and formats to append.</param>
    /// <param name="useInlineStrings">Whether to store appended text in each cell instead of the shared string table.</param>
    /// <param name="cancellationToken">Cancels the save between rows.</param>
    public Task SaveToStreamAsync(Stream stream, IAsyncEnumerable<(CellData? Data, Format? Format)[]> rows, bool useInlineStrings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(rows);

        return new XlsxWriter(this).WriteAsync(stream, rows, useInlineStrings, cancellationToken);
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
