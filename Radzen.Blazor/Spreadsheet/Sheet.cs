using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a sheet in a spreadsheet.
/// </summary>
public partial class Sheet
{
    private readonly CellDependencyGraph graph = new();

    /// <summary>
    /// Gets a value indicating whether the sheet is currently being updated.
    /// </summary>
    public bool IsUpdating { get; private set; }

    private bool isEvaluating;

    /// <summary>
    /// Gets the number of rows in the sheet.
    /// </summary>
    public int RowCount { get; private set; }
    /// <summary>
    /// Gets the number of columns in the sheet.
    /// </summary>
    public int ColumnCount { get; private set; }
    /// <summary>
    /// Gets the collection of cells in the sheet.
    /// </summary>
    public CellStore Cells { get; protected set; }
    /// <summary>
    /// Gets the selection object that manages the selected cells in the sheet.
    /// </summary>
    public Selection Selection { get; }
    /// <summary>
    /// Gets the editor object that manages cell editing in the sheet.
    /// </summary>
    public Editor Editor { get; }
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
    /// Gets the undo/redo stack for the sheet, which allows tracking and reverting changes made to the sheet.
    /// </summary>
    public UndoRedoStack Commands { get; }
    /// <summary>
    /// Gets the name of the sheet.
    /// </summary>
    public string Name { get; internal set; } = "Sheet1";
    private readonly List<Table> tables = [];

    /// <summary>
    /// Gets the list of tables associated with the sheet.
    /// </summary>
    public IReadOnlyList<Table> Tables => tables;

    private readonly HashSet<int> filteredColumns = new();

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
    /// Initializes a new instance of the <see cref="Sheet"/> class with the specified number of rows and columns.
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    public Sheet(int rows, int columns)
    {
        Rows = new(24, rows);
        Columns = new(100, columns);
        Rows.Offset = 24;
        Columns.Offset = 100;
        RowCount = rows;
        ColumnCount = columns;
        Selection = new(this);
        Editor = new(this);
        MergedCells = new(this);
        Cells = new CellStore(this);
        Commands = new();
        Validation = new();
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

        if (tree == null)
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
            var visitor = new FormulaEvaluator(this);
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

                    result.Append(value.Replace(rowDelimiter, " ").Replace(cellDelimiter, " "));
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
        var rows = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        var rowCount = Math.Min(rows.Length, RowCount - address.Row);

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var cells = rows[rowIndex].Split([cellDelimiter], StringSplitOptions.None);
            var columnCount = Math.Min(cells.Length, ColumnCount - address.Column);

            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                if (Cells.TryGet(address.Row + rowIndex, address.Column + columnIndex, out var cell))
                {
                    cell.SetValue(cells[columnIndex]);
                }
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

        // Shift cell contents left for all rows starting at deleted column
        for (var row = 0; row < RowCount; row++)
        {
            for (var col = columnIndex; col < ColumnCount - 1; col++)
            {
                var target = Cells[row, col];
                var source = Cells[row, col + 1];
                target.CopyFrom(source);
            }

            // Clear last column (now out of logical bounds)
            var last = Cells[row, ColumnCount - 1];
            last.Formula = null;
            last.Data = new CellData(null);
        }

        // Decrease column count
        ColumnCount--;

        // Adjust formulas to reflect the deletion
        AdjustFormulas((cellToken) =>
        {
            var address = cellToken.AddressValue;
            var newColumn = address.Column;

            if (newColumn > columnIndex)
            {
                newColumn -= 1;
            }
            // Keep the same index if it equals the deleted column (shifting semantics)

            // Clamp to new bounds
            if (newColumn >= ColumnCount)
            {
                newColumn = ColumnCount - 1;
            }

            return new CellRef(address.Row, Math.Max(0, newColumn));
        });

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

        // Shift cell contents up for all columns starting at deleted row
        for (var col = 0; col < ColumnCount; col++)
        {
            for (var row = rowIndex; row < RowCount - 1; row++)
            {
                var target = Cells[row, col];
                var source = Cells[row + 1, col];
                target.CopyFrom(source);
            }

            // Clear last row (now out of logical bounds)
            var last = Cells[RowCount - 1, col];
            last.Formula = null;
            last.Data = new CellData(null);
        }

        // Decrease row count
        RowCount--;

        // Adjust formulas to reflect the deletion
        AdjustFormulas((cellToken) =>
        {
            var address = cellToken.AddressValue;
            var newRow = address.Row;

            if (newRow > rowIndex)
            {
                newRow -= 1;
            }
            // Keep the same index if it equals the deleted row (shifting semantics)

            // Clamp to new bounds
            if (newRow >= RowCount)
            {
                newRow = RowCount - 1;
            }

            return new CellRef(Math.Max(0, newRow), address.Column);
        });

        EndUpdate();
    }

    private void AdjustFormulas(Func<FormulaToken, CellRef> adjust)
    {
        // Iterate through all cells to update their formulas
        for (var row = 0; row < RowCount; row++)
        {
            for (var col = 0; col < ColumnCount; col++)
            {
                var cell = Cells[row, col];

                if (cell.FormulaSyntaxTree == null || string.IsNullOrEmpty(cell.Formula))
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

            if (token.IsColumnAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(ColumnRef.ToString(adjusted.Column));
            if (token.IsRowAbsolute)
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

            if (startToken.IsColumnAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(ColumnRef.ToString(startCell.Column));
            if (startToken.IsRowAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(startCell.Row + 1);

            builder.Append(':');

            if (endToken.IsColumnAbsolute)
            {
                builder.Append('$');
            }
            builder.Append(ColumnRef.ToString(endCell.Column));
            if (endToken.IsRowAbsolute)
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
                _ => throw new InvalidOperationException($"Unsupported operator token: {token.Type}")
            };
        }
    }

    internal IEnumerable<RangeInfo> GetRanges(RangeRef range)
    {
        if (range == RangeRef.Invalid)
        {
            yield break;
        }

        var bottomRightRange = new RangeRef(new CellRef(Rows.Frozen, Columns.Frozen), new CellRef(Rows.Count - 1, Columns.Count - 1));

        if (range.Overlaps(bottomRightRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(bottomRightRange),
                FrozenRow = false,
                FrozenColumn = false,
                Top = range.Start.Row >= Rows.Frozen,
                Left = range.Start.Column >= Columns.Frozen,
                Bottom = true,
                Right = true
            };
        }

        var topLeftRange = Rows.Frozen > 0 && Columns.Frozen > 0
            ? new RangeRef(new CellRef(0, 0), new CellRef(Rows.Frozen - 1, Columns.Frozen - 1))
            : RangeRef.Invalid;

        if (range.Overlaps(topLeftRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(topLeftRange),
                FrozenRow = true,
                FrozenColumn = true,
                Top = true,
                Left = true,
                Bottom = range.End.Row < Rows.Frozen,
                Right = range.End.Column < Columns.Frozen
            };
        }

        var topRightRange = Rows.Frozen > 0
            ? new RangeRef(new CellRef(0, Columns.Frozen), new CellRef(Rows.Frozen - 1, ColumnCount - 1))
            : RangeRef.Invalid;

        if (range.Overlaps(topRightRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(topRightRange),
                FrozenRow = true,
                FrozenColumn = false,
                Top = true,
                Left = range.Start.Column >= Columns.Frozen,
                Bottom = range.End.Row < Rows.Frozen,
                Right = true
            };
        }

        var bottomLeftRange = Columns.Frozen > 0
            ? new RangeRef(new CellRef(Rows.Frozen, 0), new CellRef(RowCount - 1, Columns.Frozen - 1))
            : RangeRef.Invalid;

        if (range.Overlaps(bottomLeftRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(bottomLeftRange),
                FrozenRow = false,
                FrozenColumn = true,
                Top = range.Start.Row >= Rows.Frozen,
                Left = true,
                Bottom = true,
                Right = range.End.Column < Columns.Frozen
            };
        }
    }
}