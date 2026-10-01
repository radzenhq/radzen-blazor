using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

internal class CellDependencyGraph
{
    private readonly Dictionary<Cell, HashSet<Cell>> dependencies = [];
    private readonly Dictionary<Cell, HashSet<Cell>> dependents = [];
    private readonly Dictionary<Cell, List<AxisDependency>> axisDependencies = [];
    private readonly Dictionary<Worksheet, List<AxisDependency>> axisDependents = [];

    private IEnumerable<Cell> GetDependentCells(Cell cell)
    {
        dependents.TryGetValue(cell, out var cells);

        if (!axisDependents.TryGetValue(cell.Worksheet, out var axis) || axis.Count == 0)
        {
            return cells ?? [];
        }

        var result = cells is null ? new HashSet<Cell>() : new HashSet<Cell>(cells);

        foreach (var dependency in axis)
        {
            if (dependency.Contains(cell.Address))
            {
                result.Add(dependency.Dependent);
            }
        }

        return result;
    }

    public bool HasDependents(Cell cell)
    {
        if (dependents.TryGetValue(cell, out var cells) && cells.Count > 0)
        {
            return true;
        }

        if (axisDependents.TryGetValue(cell.Worksheet, out var axis))
        {
            foreach (var dependency in axis)
            {
                if (dependency.Contains(cell.Address))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public IEnumerable<Cell> GetTopologicallySortedDependencies(Cell cell) => GetTopologicallySortedDependencies(GetDependentCells(cell));

    public IEnumerable<Cell> GetTopologicallySortedDependencies() => GetTopologicallySortedDependencies(dependencies.Keys);

    public IEnumerable<Cell> FormulaCells => dependencies.Keys;

    public List<Cell> GetTopologicallySortedDependencies(IEnumerable<Cell> cells)
    {
        var visited = new HashSet<Cell>();
        var result = new List<Cell>();
        var stack = new Stack<(Cell Cell, bool ChildrenExpanded)>();

        foreach (var cell in cells)
        {
            if (visited.Contains(cell))
            {
                continue;
            }

            stack.Push((cell, false));

            while (stack.Count > 0)
            {
                var (current, childrenExpanded) = stack.Pop();

                if (childrenExpanded)
                {
                    result.Add(current);
                    continue;
                }

                if (!visited.Add(current))
                {
                    continue;
                }

                stack.Push((current, true));

                var children = GetDependentCells(current).ToList();
                for (var i = children.Count - 1; i >= 0; i--)
                {
                    var child = children[i];
                    if (!visited.Contains(child))
                    {
                        stack.Push((child, false));
                    }
                }
            }
        }

        result.Reverse();

        return result;
    }

    public void Remove(Cell cell)
    {
        if (axisDependencies.Remove(cell, out var oldAxisDependencies))
        {
            foreach (var dependency in oldAxisDependencies)
            {
                axisDependents[dependency.Worksheet].Remove(dependency);
            }
        }

        if (!dependencies.Remove(cell, out var oldDependencies))
        {
            return;
        }

        foreach (var dependency in oldDependencies)
        {
            if (dependents.TryGetValue(dependency, out var dependentCells))
            {
                dependentCells.Remove(cell);
            }
        }
    }

    public void Add(Cell cell)
    {
        Remove(cell);

        if (cell.Formula is null)
        {
            return;
        }

        var tree = cell.FormulaSyntaxTree ?? FormulaParser.Parse(cell.Formula);
        var visitor = new DependencyVisitor(cell.Worksheet);
        tree.Root.Accept(visitor);

        var newDependencies = visitor.Dependencies;
        dependencies[cell] = newDependencies;

        foreach (var dependency in newDependencies)
        {
            if (!dependents.TryGetValue(dependency, out var dependentCells))
            {
                dependentCells = [];
                dependents[dependency] = dependentCells;
            }
            dependentCells.Add(cell);
        }

        if (visitor.AxisRanges.Count == 0)
        {
            return;
        }

        var cellAxisDependencies = new List<AxisDependency>();

        foreach (var (worksheet, kind, start, end) in visitor.AxisRanges)
        {
            var dependency = new AxisDependency(worksheet, kind, start, end, cell);

            if (!axisDependents.TryGetValue(worksheet, out var sheetDependents))
            {
                sheetDependents = [];
                axisDependents[worksheet] = sheetDependents;
            }

            sheetDependents.Add(dependency);
            cellAxisDependencies.Add(dependency);
        }

        axisDependencies[cell] = cellAxisDependencies;
    }
}

internal sealed class AxisDependency(Worksheet worksheet, RangeKind kind, int start, int end, Cell dependent)
{
    public Worksheet Worksheet { get; } = worksheet;

    public Cell Dependent { get; } = dependent;

    public bool Contains(CellRef address)
    {
        var index = kind == RangeKind.Columns ? address.Column : address.Row;

        return index >= start && index <= end;
    }
}

class DependencyVisitor(Worksheet sheet) : IFormulaSyntaxNodeVisitor
{
    private readonly Worksheet sheet = sheet;

    public HashSet<Cell> Dependencies { get; } = [];

    public List<(Worksheet Worksheet, RangeKind Kind, int Start, int End)> AxisRanges { get; } = [];

    private readonly HashSet<string> nameStack = new(StringComparer.OrdinalIgnoreCase);

    public void VisitName(NameSyntaxNode nameSyntaxNode)
    {
        var tree = sheet.Workbook.ResolveDefinedName(nameSyntaxNode.Name);

        if (tree is null || tree.Errors.Count > 0 || !nameStack.Add(nameSyntaxNode.Name))
        {
            return;
        }

        tree.Root.Accept(this);
        nameStack.Remove(nameSyntaxNode.Name);
    }

    public void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
    {
    }

    public void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode)
    {
    }

    public void VisitBooleanLiteral(BooleanLiteralSyntaxNode booleanLiteralSyntaxNode)
    {
    }

    public void VisitUnaryExpression(UnaryExpressionSyntaxNode unaryExpressionSyntaxNode)
    {
        unaryExpressionSyntaxNode.Operand.Accept(this);
    }

    public void VisitErrorLiteral(ErrorLiteralSyntaxNode errorLiteralSyntaxNode)
    {
    }

    public void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        binaryExpressionSyntaxNode.Left.Accept(this);
        binaryExpressionSyntaxNode.Right.Accept(this);
    }

    private Worksheet? ResolveSheet(string? worksheetName)
    {
        if (string.IsNullOrEmpty(worksheetName))
        {
            return sheet;
        }

        var target = sheet.Workbook.GetSheet(worksheetName);

        return string.Equals(target?.Name, worksheetName, StringComparison.OrdinalIgnoreCase) ? target : null;
    }

    public void VisitCell(CellSyntaxNode cellIdentifierSyntaxNode)
    {
        var address = cellIdentifierSyntaxNode.Token.Address;
        var targetSheet = ResolveSheet(address.Worksheet);

        if (targetSheet is null)
        {
            return;
        }

        if (address.Row >= targetSheet.RowCount || address.Column >= targetSheet.ColumnCount)
        {
            return;
        }

        Dependencies.Add(targetSheet.Cells[address]);
    }

    public void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
    {
        foreach (var argument in functionSyntaxNode.Arguments)
        {
            argument.Accept(this);
        }
    }

    public void VisitRange(RangeSyntaxNode rangeSyntaxNode)
    {
        var startAddress = rangeSyntaxNode.Start.Token.Address;
        var endAddress = rangeSyntaxNode.End.Token.Address;
        var targetSheet = ResolveSheet(startAddress.Worksheet);

        if (targetSheet is null)
        {
            return;
        }

        if (rangeSyntaxNode.Kind == RangeKind.Columns)
        {
            AxisRanges.Add((targetSheet, RangeKind.Columns, startAddress.Column, endAddress.Column));
            return;
        }

        if (rangeSyntaxNode.Kind == RangeKind.Rows)
        {
            AxisRanges.Add((targetSheet, RangeKind.Rows, startAddress.Row, endAddress.Row));
            return;
        }

        var startRow = Math.Min(startAddress.Row, endAddress.Row);
        var endRow = Math.Min(Math.Max(startAddress.Row, endAddress.Row), targetSheet.RowCount - 1);
        var startCol = Math.Min(startAddress.Column, endAddress.Column);
        var endCol = Math.Min(Math.Max(startAddress.Column, endAddress.Column), targetSheet.ColumnCount - 1);

        for (var row = startRow; row <= endRow; row++)
        {
            for (var col = startCol; col <= endCol; col++)
            {
                Dependencies.Add(targetSheet.Cells[new CellRef(row, col)]);
            }
        }
    }
}
