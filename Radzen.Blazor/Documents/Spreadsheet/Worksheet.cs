using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

enum FormulaAdjustment
{
    AdjustRelative,
    Preserve
}

/// <summary>
/// Represents a sheet in a spreadsheet.
/// </summary>
public partial class Worksheet
{
    private readonly CellDependencyGraph graph = new();
    private readonly HashSet<int> invalidReferenceRows = [];
    private readonly HashSet<int> invalidReferenceColumns = [];

    private int updateDepth;

    internal bool IsUpdating => updateDepth > 0;

    private bool isEvaluating;

    /// <summary>
    /// Gets the number of rows in the sheet.
    /// </summary>
    public int RowCount
    {
        get => Rows.Count;
        private set => Rows.Count = value;
    }
    /// <summary>
    /// Gets the number of columns in the sheet.
    /// </summary>
    public int ColumnCount
    {
        get => Columns.Count;
        private set => Columns.Count = value;
    }

    internal bool IsDeletedRow(int rowIndex) => invalidReferenceRows.Contains(rowIndex);

    internal bool IsDeletedColumn(int columnIndex) => invalidReferenceColumns.Contains(columnIndex);
    /// <summary>
    /// Gets the collection of cells in the sheet.
    /// </summary>
    public CellStore Cells { get; protected set; }
    /// <summary>
    /// Gets the selection object that manages the selected cells in the sheet.
    /// </summary>
    public Selection Selection { get; }
    /// <summary>
    /// Gets the row axis for the sheet, which defines the properties of the rows.
    /// </summary>
    public Axis Rows { get; }
    /// <summary>
    /// Gets the column axis for the sheet, which defines the properties of the columns.
    /// </summary>
    public Axis Columns { get; }
    /// <summary>
    /// Gets the store for merged cells in the sheet, which manages cell merging and unmerging.
    /// </summary>
    public MergedCellStore MergedCells { get; }
    /// <summary>
    /// Gets the validation store for the sheet, which manages data validation rules and errors.
    /// </summary>
    public ValidationStore Validation { get; }
    /// <summary>
    /// Gets the conditional formatting store for the sheet.
    /// </summary>
    public ConditionalFormatStore ConditionalFormats { get; }
    /// <summary>
    /// Gets or sets the sheet protection settings.
    /// </summary>
    public SheetProtection Protection { get; set; } = new();
    /// <summary>
    /// Gets the name of the sheet.
    /// </summary>
    public string Name { get; set; } = "Worksheet1";
    private readonly List<Table> tables = [];

    /// <summary>
    /// Gets the list of tables associated with the sheet.
    /// </summary>
    public IReadOnlyList<Table> Tables => tables;

    private readonly List<SheetImage> images = [];

    /// <summary>
    /// Gets the list of images associated with the sheet.
    /// </summary>
    public IReadOnlyList<SheetImage> Images => images;

    internal RangeRef? AutofillPreview
    {
        get => autofillPreview;
        set
        {
            if (autofillPreview != value)
            {
                autofillPreview = value;
                AutofillPreviewChanged?.Invoke();
            }
        }
    }

    private RangeRef? autofillPreview;

    internal event Action? AutofillPreviewChanged;

    private SheetImage? selectedImage;

    /// <summary>
    /// Gets or sets the currently selected image.
    /// </summary>
    public SheetImage? SelectedImage
    {
        get => selectedImage;
        set
        {
            if (selectedImage != value)
            {
                selectedImage = value;
                SelectedImageChanged?.Invoke();
            }
        }
    }

    internal event Action? SelectedImageChanged;

    internal event Action? ImagesChanged;

    internal event Action<IAnchoredDrawing>? DrawingGeometryChanged;

    internal void OnDrawingGeometryChanged(IAnchoredDrawing drawing) => DrawingGeometryChanged?.Invoke(drawing);

    internal void AddImage(SheetImage image)
    {
        images.Add(image);
        ImagesChanged?.Invoke();
    }

    internal bool RemoveImage(SheetImage image)
    {
        var removed = images.Remove(image);
        if (removed)
        {
            ImagesChanged?.Invoke();
        }
        return removed;
    }

    private readonly List<SheetChart> charts = [];

    /// <summary>
    /// Gets the list of charts associated with the sheet.
    /// </summary>
    public IReadOnlyList<SheetChart> Charts => charts;

    private SheetChart? selectedChart;

    /// <summary>
    /// Gets or sets the currently selected chart.
    /// </summary>
    public SheetChart? SelectedChart
    {
        get => selectedChart;
        set
        {
            if (selectedChart != value)
            {
                selectedChart = value;
                SelectedChartChanged?.Invoke();
            }
        }
    }

    internal event Action? SelectedChartChanged;

    internal event Action? ChartsChanged;

    /// <summary>
    /// Adds a chart to the sheet.
    /// </summary>
    public void AddChart(SheetChart chart)
    {
        charts.Add(chart);
        ChartsChanged?.Invoke();
    }

    /// <summary>
    /// Removes a chart from the sheet.
    /// </summary>
    public bool RemoveChart(SheetChart chart)
    {
        var removed = charts.Remove(chart);
        if (removed)
        {
            if (selectedChart == chart)
            {
                SelectedChart = null;
            }

            ChartsChanged?.Invoke();
        }

        return removed;
    }

    private readonly HashSet<int> filteredColumns = [];

    internal IReadOnlySet<int> FilteredColumns => filteredColumns;

    /// <summary>
    /// Gets the registry of formula functions available in the sheet.
    /// </summary>
    public FunctionStore FunctionRegistry { get; } = new();
   
    private Workbook? workbook;

    /// <summary>
    /// Gets the workbook that contains this sheet.
    /// </summary>
    public Workbook Workbook
    {
        get => workbook ??= new Workbook(this);
        internal set => workbook = value;
    }

    /// <summary>
    /// Returns true if the cell at the given address is editable (either protection is off or the cell is unlocked).
    /// </summary>
    public bool IsCellEditable(CellRef address)
    {
        if (!Protection.IsProtected)
        {
            return true;
        }

        return Cells.TryGet(address.Row, address.Column, out var cell) && !cell.Format.IsLocked;
    }

    /// <summary>
    /// Adds a table to the sheet.
    /// </summary>
    /// <param name="name">Unique table name (used in structured references like <c>=SUM(Sales[Amount])</c>).</param>
    /// <param name="range">The cell range covered by the table.</param>
    /// <param name="hasHeaders">When true, the first row of <paramref name="range"/> is treated as the header row.</param>
    /// <param name="totalsRowShown">When true, the last row of <paramref name="range"/> is treated as the totals row. Used by the XLSX reader; new tables typically toggle this via <see cref="Table.ShowTotals"/>.</param>
    /// <returns>The created <see cref="Table"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="range"/> is invalid or the name is already in use.</exception>
    public Table AddTable(string name, RangeRef range, bool hasHeaders = true, bool totalsRowShown = false)
    {
        if (range == RangeRef.Invalid || range.Rows == 0 || range.Columns == 0)
        {
            throw new ArgumentException("Invalid range for Table.");
        }
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (tables.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"A table named '{name}' already exists on this sheet.", nameof(name));
        }

        var table = new Table(this, name, range, hasHeaders, totalsRowShown);
        tables.Add(table);
        OnChromeChanged();
        return table;
    }

    /// <summary>
    /// Removes a table abstraction. Cell content is left in place.
    /// </summary>
    public bool RemoveTable(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var removed = tables.Remove(table);
        OnChromeChanged();
        return removed;
    }

    /// <summary>
    /// Raised when sheet chrome (table styling, custom cell types, validation, auto filter)
    /// changes and dependent cell views must refresh.
    /// </summary>
    public event Action? ChromeChanged;

    internal void OnChromeChanged()
    {
        ChromeChanged?.Invoke();
    }

    /// <summary>
    /// Gets the table containing the cell at the given row/column, or null if none.
    /// </summary>
    public Table? GetTableContaining(int row, int column)
    {
        for (int i = 0; i < tables.Count; i++)
        {
            if (tables[i].Range.Contains(row, column))
            {
                return tables[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Worksheet"/> class with the specified number of rows and columns.
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    public Worksheet(int rows, int columns)
    {
        Rows = new(22, rows);
        Columns = new(100, columns);
        Selection = new(this);
        MergedCells = new();
        Cells = new CellStore(this);
        Validation = new ValidationStore(this);
        ConditionalFormats = new();
        AutoFilter = new(this);
    }

    internal CellRef Clamp(CellRef address)
    {
        var row = Math.Max(0, Math.Min(RowCount - 1, address.Row));
        var column = Math.Max(0, Math.Min(ColumnCount - 1, address.Column));
        return new CellRef(row, column);
    }

    private void EvaluateFormula(Cell cell)
    {
        var tree = cell.FormulaSyntaxTree;

        if (tree is null)
        {
            return;
        }

        if (tree.Errors.Count > 0)
        {
            cell.Data = CellData.FromError(CellError.Name);
        }
        else
        {
            isEvaluating = true;
            try
            {
                var visitor = new FormulaEvaluator(this, cell);
                var eval = visitor.Evaluate(tree.Root);
                cell.Data = eval;
            }
            finally
            {
                isEvaluating = false;
            }
        }

        cell.OnChanged();
    }

    internal CellData EvaluateExpression(string expression)
    {
        var tree = FormulaParser.Parse(expression);

        if (tree.Errors.Count > 0)
        {
            return CellData.FromError(CellError.Name);
        }

        var context = new Cell(this, new CellRef(0, 0));
        var visitor = new FormulaEvaluator(this, context);
        return visitor.Evaluate(tree.Root);
    }

    internal void OnCellValueChanged(Cell cell)
    {
        if (isEvaluating)
        {
            return;
        }

        // During a batch, EndUpdate recalculates dependent formulas once. The changed cell
        // itself is not a formula node, so it would never be notified - fire its event here.
        if (!IsUpdating)
        {
            var dependents = graph.GetTopologicallySortedDependencies(cell);

            foreach (var dependentCell in dependents)
            {
                EvaluateFormula(dependentCell);
            }
        }

        cell.OnChanged();
    }

    internal void OnCellFormulaChanged(Cell cell)
    {
        graph.Add(cell);

        if (!IsUpdating)
        {
            // Re-evaluate the changed cell and its transitive dependents only. The per-cell
            // overload returns the dependents but not the cell itself, so evaluate it first.
            EvaluateFormula(cell);

            foreach (var c in graph.GetTopologicallySortedDependencies(cell))
            {
                EvaluateFormula(c);
            }
        }
    }

    internal void RefreshCells(RangeRef range, bool validate = false)
    {
        foreach (var cellRef in range.GetCells())
        {
            if (Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                if (validate)
                {
                    cell.Validate();
                }

                cell.OnChanged();
            }
        }
    }

    /// <summary>
    /// Begins an update operation on the sheet, suspending formula evaluation until the
    /// matching <see cref="EndUpdate"/>. Calls nest: evaluation resumes only when the
    /// outermost <see cref="EndUpdate"/> runs.
    /// </summary>
    public void BeginUpdate()
    {
        updateDepth++;
    }

    /// <summary>
    /// Ends an update operation. When this balances the outermost <see cref="BeginUpdate"/>,
    /// all formulas are re-evaluated once and pending changes are flushed. An unmatched call
    /// (no open update) is a no-op.
    /// </summary>
    public void EndUpdate()
    {
        if (updateDepth == 0)
        {
            return;
        }

        updateDepth--;

        if (updateDepth > 0)
        {
            return;
        }

        foreach (var cell in graph.GetTopologicallySortedDependencies())
        {
            EvaluateFormula(cell);
        }

        Selection.TriggerPendingChange();
    }

    /// <summary>
    /// Runs <paramref name="action"/> inside a single update batch: formula evaluation is
    /// suspended for the duration and runs once afterwards, even if <paramref name="action"/>
    /// throws. Nests safely with other batches and direct Begin/EndUpdate calls.
    /// </summary>
    public void Batch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        BeginUpdate();
        try
        {
            action();
        }
        finally
        {
            EndUpdate();
        }
    }

    /// <summary>
    /// Runs <paramref name="func"/> inside a single update batch and returns its result.
    /// Formula evaluation is suspended for the duration and runs once afterwards, even if
    /// <paramref name="func"/> throws. Nests safely with other batches.
    /// </summary>
    public T Batch<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        BeginUpdate();
        try
        {
            return func();
        }
        finally
        {
            EndUpdate();
        }
    }

    internal string GetDelimitedString(RangeRef range, string rowDelimiter = "\n", string cellDelimiter = "\t")
    {
        ArgumentNullException.ThrowIfNull(rowDelimiter);
        ArgumentNullException.ThrowIfNull(cellDelimiter);
        if (range == RangeRef.Invalid || range.Rows == 0 || range.Columns == 0)
        {
            return string.Empty;
        }

        var result = StringBuilderCache.Acquire();

        for (var row = range.Start.Row; row <= range.End.Row; row++)
        {
            for (var column = range.Start.Column; column <= range.End.Column; column++)
            {
                if (column > range.Start.Column)
                {
                    result.Append(cellDelimiter);
                }

                if (Cells.TryGet(row, column, out var cell))
                {
                    var value = cell.GetValue() ?? string.Empty;

                    result.Append(value.Replace(rowDelimiter, " ", StringComparison.Ordinal)
                        .Replace(cellDelimiter, " ", StringComparison.Ordinal));
                }
            }

            result.Append(rowDelimiter);
        }

        if (result.Length >= rowDelimiter.Length)
        {
            result.Length -= rowDelimiter.Length;
        }
        return result.ToString();
    }

    internal void InsertDelimitedString(CellRef address, string value, string cellDelimiter = "\t")
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(cellDelimiter);
        var rows = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        var rowCount = Math.Min(rows.Length, RowCount - address.Row);

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var cells = rows[rowIndex].Split([cellDelimiter], StringSplitOptions.None);
            var columnCount = Math.Min(cells.Length, ColumnCount - address.Column);

            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var cell = Cells[address.Row + rowIndex, address.Column + columnIndex];
                cell.SetValue(cells[columnIndex]);
            }
        }
    }

    /// <summary>
    /// Deletes the specified column and shifts cells left. Updates formulas and decreases column count.
    /// </summary>
    /// <param name="columnIndex"></param>
    public void DeleteColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        Batch(() => DeleteColumnCore(columnIndex));
    }

    private void DeleteColumnCore(int columnIndex)
    {
        // Sparse shift - O(populated cells) instead of O(rows x columns)
        Cells.ShiftColumnsLeft(columnIndex);

        Columns.ShiftUp(columnIndex);

        MergedCells.ShiftColumnsLeft(columnIndex);

        invalidReferenceColumns.Add(columnIndex);

        InvalidateFormulasReferencing(cellRef => cellRef.Column == columnIndex);

        ColumnCount--;

        // A different cell now occupies the selection if it sat at or after the deleted
        // column - notify so the formula bar re-reads. Skip when the delete left the
        // selection past the new bounds (re-reading there would be out of range).
        if (Selection.Cell != CellRef.Invalid && Selection.Cell.Column >= columnIndex && Selection.Cell.Column < ColumnCount)
        {
            Selection.NotifyContentChanged();
        }
    }

    /// <summary>
    /// Deletes the specified row and shifts cells up. Updates formulas and decreases row count.
    /// </summary>
    /// <param name="rowIndex"></param>
    public void DeleteRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        Batch(() => DeleteRowCore(rowIndex));
    }

    private void DeleteRowCore(int rowIndex)
    {
        // Sparse shift - O(populated cells) instead of O(rows x columns)
        Cells.ShiftRowsUp(rowIndex);

        Rows.ShiftUp(rowIndex);

        MergedCells.ShiftRowsUp(rowIndex);

        invalidReferenceRows.Add(rowIndex);

        InvalidateFormulasReferencing(cellRef => cellRef.Row == rowIndex);

        RowCount--;

        // A different cell now occupies the selection if it sat at or after the deleted
        // row - notify so the formula bar re-reads. Skip when the delete left the
        // selection past the new bounds (re-reading there would be out of range).
        if (Selection.Cell != CellRef.Invalid && Selection.Cell.Row >= rowIndex && Selection.Cell.Row < RowCount)
        {
            Selection.NotifyContentChanged();
        }
    }

    private void AdjustFormulas(Func<FormulaToken, CellRef> adjust)
    {
        // Snapshot to avoid collection-modified errors when setting Formula triggers graph updates
        foreach (var cell in graph.FormulaCells.ToList())
        {
            if (cell.FormulaSyntaxTree is null || string.IsNullOrEmpty(cell.Formula))
            {
                continue;
            }

            var newFormula = FormulaRewriter.Rewrite(cell.Formula!, cell.FormulaSyntaxTree!, adjust);

            if (!string.Equals(newFormula, cell.Formula, StringComparison.Ordinal))
            {
                cell.Formula = newFormula;
            }
        }
    }


    internal static string AdjustFormulaForCopy(string formula, int rowDelta, int colDelta)
    {
        var tree = FormulaParser.Parse(formula);

        if (tree.Errors.Count > 0)
        {
            return formula;
        }

        return FormulaRewriter.Rewrite(formula, tree, token =>
        {
            var addr = token.Address;
            var newRow = addr.IsRowAbsolute ? addr.Row : addr.Row + rowDelta;
            var newCol = addr.IsColumnAbsolute ? addr.Column : addr.Column + colDelta;
            return new CellRef(newRow, newCol) { Worksheet = addr.Worksheet };
        });
    }

    internal void PasteRange(Worksheet sourceSheet, RangeRef source, CellRef destinationStart, FormulaAdjustment adjustment)
    {
        if (source == RangeRef.Invalid)
        {
            return;
        }

        var rowDelta = destinationStart.Row - source.Start.Row;
        var colDelta = destinationStart.Column - source.Start.Column;

        List<Cell> cells;

        BeginUpdate();
        try
        {
            cells = PasteRangeCore(sourceSheet, source, rowDelta, colDelta, adjustment);
        }
        finally
        {
            EndUpdate();
        }

        foreach (var cell in cells)
        {
            cell.OnChanged();
        }
    }

    private List<Cell> PasteRangeCore(Worksheet sourceSheet, RangeRef source, int rowDelta, int colDelta, FormulaAdjustment adjustment)
    {
        var cells = new List<Cell>();

        for (var sr = source.Start.Row; sr <= source.End.Row; sr++)
        {
            for (var sc = source.Start.Column; sc <= source.End.Column; sc++)
            {
                var dr = sr + rowDelta;
                var dc = sc + colDelta;

                if (!sourceSheet.Cells.TryGet(sr, sc, out var srcCell))
                {
                    continue;
                }

                if (dr < 0 || dr >= RowCount || dc < 0 || dc >= ColumnCount)
                {
                    continue;
                }

                var dstCell = Cells[dr, dc];

                if (!string.IsNullOrEmpty(srcCell.Formula))
                {
                    var formula = srcCell.Formula!;

                    if (adjustment == FormulaAdjustment.Preserve)
                    {
                        dstCell.Formula = formula;
                    }
                    else
                    {
                        var adjusted = AdjustFormulaForCopy(formula, rowDelta, colDelta);
                        dstCell.Formula = adjusted;
                    }
                }
                else
                {
                    dstCell.Value = srcCell.Value;
                }

                cells.Add(dstCell);
            }
        }

        return cells;
    }

    internal void InvalidateFormulasReferencing(Predicate<CellRef> isInvalidated)
    {
        // Snapshot to avoid collection-modified errors when setting Formula triggers graph updates
        foreach (var cell in graph.FormulaCells.ToList())
        {
            var tree = cell.FormulaSyntaxTree;
            if (tree is null)
            {
                continue;
            }

            var hasRef = tree.Find(node => node switch
            {
                CellSyntaxNode c => isInvalidated(c.Token.Address),
                RangeSyntaxNode r => RangeContainsInvalidated(r, isInvalidated),
                _ => false,
            }).Count > 0;

            if (!hasRef)
            {
                continue;
            }

            var tokens = FormulaLexer.Scan(cell.Formula!, false);
            for (var i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Type == FormulaTokenType.CellIdentifier && isInvalidated(t.Address))
                {
                    tokens[i] = new FormulaToken(FormulaTokenType.ErrorLiteral, "#REF!") { ErrorValue = CellError.Ref };
                }
            }

            var rebuilt = StringBuilderCache.Acquire();
            foreach (var t in tokens)
            {
                if (t.Type == FormulaTokenType.None)
                {
                    break;
                }

                rebuilt.Append(t.Value);
            }
            cell.Formula = StringBuilderCache.GetStringAndRelease(rebuilt);
        }
    }

    private static bool RangeContainsInvalidated(RangeSyntaxNode range, Predicate<CellRef> isInvalidated)
    {
        var start = range.Start.Token.Address;
        var end = range.End.Token.Address;
        for (var r = start.Row; r <= end.Row; r++)
        {
            for (var c = start.Column; c <= end.Column; c++)
            {
                if (isInvalidated(new CellRef(r, c)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Inserts one or more rows at the specified index and shifts existing cells down. Updates formulas and increases row count.
    /// </summary>
    public void InsertRow(int rowIndex, int count = 1)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rowIndex, RowCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
#else
        if (rowIndex < 0 || rowIndex > RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
#endif

        Batch(() => InsertRowCore(rowIndex, count));
    }

    private void InsertRowCore(int rowIndex, int count)
    {
        // Sparse shift - O(populated cells) instead of O(rows x columns)
        Cells.ShiftRowsDown(rowIndex, count);

        Rows.ShiftDown(rowIndex, count);

        MergedCells.ShiftRowsDown(rowIndex, count);

        AdjustFormulas((cellToken) =>
        {
            var a = cellToken.Address;
            var newRow = a.Row >= rowIndex ? a.Row + count : a.Row;
            return new CellRef(newRow, a.Column);
        });

        // Increase the row count last so the axis change event fires after every shift
        // is in place. Doing it first would re-render the grid (and the selection overlay)
        // against intermediate geometry with nothing re-rendering afterwards.
        RowCount += count;

        // The selection stays at the same address, but if it sits at or below the insert
        // point a different cell now occupies it - notify so the formula bar re-reads.
        if (Selection.Cell != CellRef.Invalid && Selection.Cell.Row >= rowIndex)
        {
            Selection.NotifyContentChanged();
        }
    }

    /// <summary>
    /// Inserts one or more columns at the specified index and shifts existing cells right. Updates formulas and increases column count.
    /// </summary>
    public void InsertColumn(int columnIndex, int count = 1)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columnIndex, ColumnCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
#else
        if (columnIndex < 0 || columnIndex > ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
#endif

        Batch(() => InsertColumnCore(columnIndex, count));
    }

    private void InsertColumnCore(int columnIndex, int count)
    {
        // Sparse shift - O(populated cells) instead of O(rows x columns)
        Cells.ShiftColumnsRight(columnIndex, count);

        Columns.ShiftDown(columnIndex, count);

        MergedCells.ShiftColumnsRight(columnIndex, count);

        AdjustFormulas((cellToken) =>
        {
            var a = cellToken.Address;
            var newCol = a.Column >= columnIndex ? a.Column + count : a.Column;
            return new CellRef(a.Row, newCol);
        });

        // Increase the column count last so the axis change event fires after every shift
        // is in place. Doing it first would re-render the grid (and the selection overlay)
        // against intermediate geometry with nothing re-rendering afterwards.
        ColumnCount += count;

        // The selection stays at the same address, but if it sits at or after the insert
        // point a different cell now occupies it - notify so the formula bar re-reads.
        if (Selection.Cell != CellRef.Invalid && Selection.Cell.Column >= columnIndex)
        {
            Selection.NotifyContentChanged();
        }
    }
    class FormulaRewriter : FormulaSyntaxNodeVisitorBase
    {
        private readonly Func<FormulaToken, CellRef> adjust;
        private readonly StringBuilder builder = new();

        private FormulaRewriter(Func<FormulaToken, CellRef> adjust)
        {
            this.adjust = adjust;
        }

        public static string Rewrite(string original, FormulaSyntaxTree tree, Func<FormulaToken, CellRef> adjust)
        {
            var rewriter = new FormulaRewriter(adjust);
            rewriter.builder.Append('=');
            tree.Root.Accept(rewriter);
            return rewriter.builder.ToString();
        }

        public override void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
        {
            builder.Append(numberLiteralSyntaxNode.Token.Value);
        }

        public override void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode)
        {
            builder.Append('"');
            builder.Append(stringLiteralSyntaxNode.Token.Value);
            builder.Append('"');
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
        {
            binaryExpressionSyntaxNode.Left.Accept(this);
            builder.Append(TokenToOperator(binaryExpressionSyntaxNode.Token));
            binaryExpressionSyntaxNode.Right.Accept(this);
        }

        public override void VisitCell(CellSyntaxNode cellSyntaxNode)
        {
            var token = cellSyntaxNode.Token;
            var adjusted = adjust(token);

            AppendWorksheetPrefix(token.Address.Worksheet);

            if (token.Address.IsColumnAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(ColumnRef.ToString(adjusted.Column));
            if (token.Address.IsRowAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(adjusted.Row + 1);
        }

        public override void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
        {
            builder.Append(functionSyntaxNode.Name);
            builder.Append('(');
            for (int i = 0; i < functionSyntaxNode.Arguments.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }
                functionSyntaxNode.Arguments[i].Accept(this);
            }
            builder.Append(')');
        }

        public override void VisitRange(RangeSyntaxNode rangeSyntaxNode)
        {
            var startToken = rangeSyntaxNode.Start.Token;
            var endToken = rangeSyntaxNode.End.Token;

            var startAdjusted = adjust(startToken);
            var endAdjusted = adjust(endToken);

            // Ensure start <= end after adjustments
            var startCell = startAdjusted;
            var endCell = endAdjusted;

            if (startCell.Row > endCell.Row || (startCell.Row == endCell.Row && startCell.Column > endCell.Column))
            {
                (startCell, endCell) = CellRef.Swap(startCell, endCell);
            }

            AppendWorksheetPrefix(startToken.Address.Worksheet);

            if (startToken.Address.IsColumnAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(ColumnRef.ToString(startCell.Column));
            if (startToken.Address.IsRowAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(startCell.Row + 1);

            builder.Append(':');

            if (endToken.Address.IsColumnAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(ColumnRef.ToString(endCell.Column));
            if (endToken.Address.IsRowAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(endCell.Row + 1);
        }

        private void AppendWorksheetPrefix(string? worksheet)
        {
            if (string.IsNullOrEmpty(worksheet))
            {
                return;
            }

            if (NeedsQuoting(worksheet))
            {
                builder.Append('\'');
                builder.Append(worksheet.Replace("'", "''", StringComparison.Ordinal));
                builder.Append('\'');
            }
            else
            {
                builder.Append(worksheet);
            }

            builder.Append('!');
        }

        private static bool NeedsQuoting(string name)
        {
            if (char.IsDigit(name[0]))
            {
                return true;
            }

            foreach (var ch in name)
            {
                if (ch == ' ' || ch == '\'')
                {
                    return true;
                }
            }

            return false;
        }

        private static string TokenToOperator(FormulaToken token)
        {
            return token.Type switch
            {
                FormulaTokenType.Plus => "+",
                FormulaTokenType.Minus => "-",
                FormulaTokenType.Star => "*",
                FormulaTokenType.Slash => "/",
                FormulaTokenType.Equals => "=",
                FormulaTokenType.GreaterThan => ">",
                FormulaTokenType.GreaterThanOrEqual => ">=",
                FormulaTokenType.LessThan => "<",
                FormulaTokenType.LessThanOrEqual => "<=",
                FormulaTokenType.EqualsGreaterThan => ">=",
                FormulaTokenType.LessThanGreaterThan => "<>",
                _ => throw new InvalidOperationException($"Unsupported operator token: {token.Type}")
            };
        }
    }

}