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

    /// <summary>
    /// Gets a value indicating whether the sheet is currently being updated.
    /// </summary>
    public bool IsUpdating { get; private set; }

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
    /// Gets the name of the sheet.
    /// </summary>
    public string Name { get; internal set; } = "Worksheet1";
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

    /// <summary>
    /// Occurs when the selected image changes.
    /// </summary>
    internal event Action? SelectedImageChanged;

    internal event Action? ImagesChanged;

    internal void AddImage(SheetImage image) { images.Add(image); ImagesChanged?.Invoke(); }

    internal bool RemoveImage(SheetImage image) { var removed = images.Remove(image); if (removed) ImagesChanged?.Invoke(); return removed; }

    private readonly HashSet<int> filteredColumns = [];

    /// <summary>
    /// Gets the set of columns that have filters applied.
    /// </summary>
    public IReadOnlySet<int> FilteredColumns => filteredColumns;

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
    /// Adds a table to the sheet.
    /// </summary>
    /// <param name="range"></param>
    /// <exception cref="ArgumentException"></exception>
    public void AddTable(RangeRef range)
    {
        if (range == RangeRef.Invalid || range.Rows == 0 || range.Columns == 0)
        {
            throw new ArgumentException("Invalid range for Table.");
        }
        tables.Add(new Table(this, range));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Worksheet"/> class with the specified number of rows and columns.
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    public Worksheet(int rows, int columns)
    {
        Rows = new(24, rows);
        Columns = new(100, columns);
        Selection = new(this);
        MergedCells = new();
        Cells = new CellStore(this);
        Validation = new();
        ConditionalFormats = new();
    }

    /// <summary>
    /// Clamps the specified cell address to ensure it is within the bounds of the sheet.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public CellRef Clamp(CellRef address)
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
            var visitor = new FormulaEvaluator(this, cell);
            var eval = visitor.Evaluate(tree.Root);
            cell.Data = eval;
            isEvaluating = false;
        }

        cell.OnChanged();
    }

    internal void OnCellValueChanged(Cell cell)
    {
        if (!IsUpdating && !isEvaluating)
        {
            var dependents = graph.GetTopologicallySortedDependencies(cell);

            foreach (var dependentCell in dependents)
            {
                EvaluateFormula(dependentCell);
            }

            cell.OnChanged();
        }
    }

    internal void OnCellFormulaChanged(Cell cell)
    {
        graph.Add(cell);

        if (!IsUpdating)
        {
            foreach (var c in graph.GetTopologicallySortedDependencies())
            {
                EvaluateFormula(c);
            }
        }
    }

    /// <summary>
    /// Notifies populated cells in the given range that they should re-validate and/or refresh.
    /// </summary>
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
    /// Begins an update operation on the sheet, preventing immediate evaluation of formulas and changes.
    /// </summary>
    public void BeginUpdate()
    {
        IsUpdating = true;
    }

    /// <summary>
    /// Ends the update operation on the sheet, triggering evaluation of all formulas and pending changes.
    /// </summary>
    public void EndUpdate()
    {
        IsUpdating = false;

        foreach (var cell in graph.GetTopologicallySortedDependencies())
        {
            EvaluateFormula(cell);
        }

        Selection.TriggerPendingChange();
    }

    /// <summary>
    /// Gets a delimited string representation of the specified range in the sheet.
    /// </summary>
    /// <param name="range"></param>
    /// <param name="rowDelimiter"></param>
    /// <param name="cellDelimiter"></param>
    /// <returns></returns>
    public string GetDelimitedString(RangeRef range, string rowDelimiter = "\n", string cellDelimiter = "\t")
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

        return result.ToString().TrimEnd(rowDelimiter.ToCharArray());
    }

    /// <summary>
    /// Inserts a delimited string into the specified cell address in the sheet, splitting the string into rows and cells based on the specified delimiters.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    /// <param name="cellDelimiter"></param>

    public void InsertDelimitedString(CellRef address, string value, string cellDelimiter = "\t")
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

        BeginUpdate();

        // Shift cells left using sparse operation - O(populated cells) instead of O(rows × columns)
        Cells.ShiftColumnsLeft(columnIndex);

        // Shift column metadata (custom widths, hidden state)
        Columns.ShiftUp(columnIndex);

        // Shift merged cell ranges
        MergedCells.ShiftColumnsLeft(columnIndex);

        // Mark deleted column index as invalid for references
        invalidReferenceColumns.Add(columnIndex);

        // Any formulas that reference this column should be turned into =#REF!
        InvalidateFormulasReferencing(column: columnIndex);

        // Decrease column count
        ColumnCount--;

        EndUpdate();
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

        BeginUpdate();

        // Shift cells up using sparse operation - O(populated cells) instead of O(rows × columns)
        Cells.ShiftRowsUp(rowIndex);

        // Shift row metadata (custom heights, hidden state)
        Rows.ShiftUp(rowIndex);

        // Shift merged cell ranges
        MergedCells.ShiftRowsUp(rowIndex);

        // Mark deleted row index as invalid for references
        invalidReferenceRows.Add(rowIndex);

        // Any formulas that reference this row should be turned into =#REF!
        InvalidateFormulasReferencing(row: rowIndex);

        // Decrease row count
        RowCount--;

        EndUpdate();
    }

    private void AdjustFormulas(Func<FormulaToken, CellRef> adjust)
    {
        // Snapshot to avoid collection-modified errors when setting Formula triggers graph updates
        foreach (var cell in Cells.GetPopulatedCells().ToList())
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
        // Expect formula starts with '='; lexer scans tokens including '=' and cells
        var tokens = FormulaLexer.Scan(formula, false);

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == FormulaTokenType.CellIdentifier)
            {
                var addr = t.Address;
                var newRow = addr.IsRowAbsolute ? addr.Row : addr.Row + rowDelta;
                var newCol = addr.IsColumnAbsolute ? addr.Column : addr.Column + colDelta;

                var sb = new StringBuilder();
                if (addr.IsColumnAbsolute)
                {
                    sb.Append('$');
                }
                sb.Append(ColumnRef.ToString(newCol));
                if (addr.IsRowAbsolute)
                {
                    sb.Append('$');
                }
                sb.Append(newRow + 1);

                t.Value = sb.ToString();
                tokens[i] = t;
            }
        }

        var result = new StringBuilder();
        foreach (var t in tokens)
        {
            if (t.Type == FormulaTokenType.None)
            {
                break;
            }
            result.Append(t.Value);
        }
        return result.ToString();
    }

    internal void PasteRange(Worksheet sourceSheet, RangeRef source, CellRef destinationStart, FormulaAdjustment adjustment)
    {
        if (source == RangeRef.Invalid)
        {
            return;
        }

        var rowDelta = destinationStart.Row - source.Start.Row;
        var colDelta = destinationStart.Column - source.Start.Column;

        BeginUpdate();

        var cells  = new List<Cell>();

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

        EndUpdate();

        foreach (var cell in cells)
        {
            cell.OnChanged();
        }
    }

    private void InvalidateFormulasReferencing(int? row = null, int? column = null)
    {
        // Snapshot to avoid collection-modified errors when setting Formula triggers graph updates
        foreach (var cell in Cells.GetPopulatedCells().ToList())
        {
            var tree = cell.FormulaSyntaxTree;
            if (tree is null) continue;

            bool hasRef;

            if (row.HasValue)
            {
                var rowIndex = row.Value;
                hasRef = tree.Find(node => node is CellSyntaxNode c && c.Token.Address.Row == rowIndex
                    || node is RangeSyntaxNode r && r.Start.Token.Address.Row <= rowIndex && r.End.Token.Address.Row >= rowIndex).Count > 0;
            }
            else
            {
                var columnIndex = column!.Value;
                hasRef = tree.Find(node => node is CellSyntaxNode c && c.Token.Address.Column == columnIndex
                    || node is RangeSyntaxNode r && r.Start.Token.Address.Column <= columnIndex && r.End.Token.Address.Column >= columnIndex).Count > 0;
            }

            if (hasRef)
            {
                var tokens = FormulaLexer.Scan(cell.Formula!, false);
                for (int i = 0; i < tokens.Count; i++)
                {
                    var t = tokens[i];
                    if (t.Type == FormulaTokenType.CellIdentifier)
                    {
                        if ((row.HasValue && t.Address.Row == row.Value) ||
                            (column.HasValue && t.Address.Column == column.Value))
                        {
                            tokens[i] = new FormulaToken(FormulaTokenType.ErrorLiteral, "#REF!") { ErrorValue = CellError.Ref };
                        }
                    }
                }
                var rebuilt = new StringBuilder();
                foreach (var t in tokens)
                {
                    if (t.Type == FormulaTokenType.None) break;
                    rebuilt.Append(t.Value);
                }
                cell.Formula = rebuilt.ToString();
            }
        }
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

        BeginUpdate();

        RowCount += count;

        // Shift cells down using sparse operation - O(populated cells) instead of O(rows × columns)
        Cells.ShiftRowsDown(rowIndex, count);

        // Shift row metadata (custom heights, hidden state)
        Rows.ShiftDown(rowIndex, count);

        // Shift merged cell ranges
        MergedCells.ShiftRowsDown(rowIndex, count);

        // Adjust formulas: shift row indices at or after the insert point
        AdjustFormulas((cellToken) =>
        {
            var a = cellToken.Address;
            var newRow = a.Row >= rowIndex ? a.Row + count : a.Row;
            return new CellRef(newRow, a.Column);
        });

        EndUpdate();
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

        BeginUpdate();

        ColumnCount += count;

        // Shift cells right using sparse operation - O(populated cells) instead of O(rows × columns)
        Cells.ShiftColumnsRight(columnIndex, count);

        // Shift column metadata (custom widths, hidden state)
        Columns.ShiftDown(columnIndex, count);

        // Shift merged cell ranges
        MergedCells.ShiftColumnsRight(columnIndex, count);

        // Adjust formulas: shift column indices at or after the insert point
        AdjustFormulas((cellToken) =>
        {
            var a = cellToken.Address;
            var newCol = a.Column >= columnIndex ? a.Column + count : a.Column;
            return new CellRef(a.Row, newCol);
        });

        EndUpdate();
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
            // Always start with '=' for formulas
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
            // Adjust both sides using the provided delegate
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