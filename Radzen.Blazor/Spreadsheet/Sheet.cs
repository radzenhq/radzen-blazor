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
        var node = cell.FormulaSyntaxNode;

        if (node == null)
        {
            return;
        }

        isEvaluating = true;
        var visitor = new FormulaEvaluator(this);
        cell.Value = visitor.Evaluate(node);
        isEvaluating = false;

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