using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

internal class CellDependencyGraph
{
    private readonly Dictionary<Cell, HashSet<Cell>> dependencies = [];
    private readonly Dictionary<Cell, HashSet<Cell>> dependents = [];
    private readonly Dictionary<Cell, List<RangeDependency>> rangeDependencies = [];
    private readonly Dictionary<RangeArea, RangeDependency> ranges = [];
    private readonly Dictionary<Worksheet, RangeIndex> indexes = [];

    public bool HasDependents(Cell cell)
    {
        if (dependents.TryGetValue(cell, out var cells) && cells.Count > 0)
        {
            return true;
        }

        if (indexes.TryGetValue(cell.Worksheet, out var index))
        {
            foreach (var _ in index.Find(cell.Address))
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<Cell> GetTopologicallySortedDependencies(Cell cell)
    {
        var visited = new HashSet<object> { cell };
        var result = new List<Cell>();
        var stack = new Stack<(object Node, bool ChildrenExpanded)>();

        PushChildren(stack, visited, cell);
        Sort(stack, visited, result);

        result.Reverse();

        return result;
    }

    public IEnumerable<Cell> GetTopologicallySortedDependencies() => GetTopologicallySortedDependencies(dependencies.Keys);

    public IEnumerable<Cell> FormulaCells => dependencies.Keys;

    public List<Cell> GetTopologicallySortedDependencies(IEnumerable<Cell> cells)
    {
        var visited = new HashSet<object>();
        var result = new List<Cell>();
        var stack = new Stack<(object Node, bool ChildrenExpanded)>();

        foreach (var cell in cells)
        {
            if (visited.Contains(cell))
            {
                continue;
            }

            stack.Push((cell, false));
            Sort(stack, visited, result);
        }

        result.Reverse();

        return result;
    }

    private void Sort(Stack<(object Node, bool ChildrenExpanded)> stack, HashSet<object> visited, List<Cell> result)
    {
        while (stack.Count > 0)
        {
            var (current, childrenExpanded) = stack.Pop();

            if (childrenExpanded)
            {
                if (current is Cell cell)
                {
                    result.Add(cell);
                }

                continue;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            stack.Push((current, true));

            if (current is Cell currentCell)
            {
                PushChildren(stack, visited, currentCell);
            }
            else
            {
                foreach (var dependent in ((RangeDependency)current).Dependents)
                {
                    if (!visited.Contains(dependent))
                    {
                        stack.Push((dependent, false));
                    }
                }
            }
        }
    }

    private void PushChildren(Stack<(object Node, bool ChildrenExpanded)> stack, HashSet<object> visited, Cell cell)
    {
        if (indexes.TryGetValue(cell.Worksheet, out var index))
        {
            foreach (var range in index.Find(cell.Address))
            {
                if (!visited.Contains(range))
                {
                    stack.Push((range, false));
                }
            }
        }

        if (dependents.TryGetValue(cell, out var cells))
        {
            foreach (var dependent in cells)
            {
                if (!visited.Contains(dependent))
                {
                    stack.Push((dependent, false));
                }
            }
        }
    }

    public void Remove(Cell cell)
    {
        if (rangeDependencies.Remove(cell, out var oldRanges))
        {
            foreach (var range in oldRanges)
            {
                range.Dependents.Remove(cell);

                if (range.Dependents.Count == 0)
                {
                    ranges.Remove(range.Area);
                    indexes[range.Area.Worksheet].Remove(range);
                }
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

        if (visitor.Ranges.Count == 0)
        {
            return;
        }

        var cellRanges = new List<RangeDependency>();

        foreach (var area in visitor.Ranges)
        {
            if (!ranges.TryGetValue(area, out var range))
            {
                range = new RangeDependency(area);
                ranges[area] = range;

                if (!indexes.TryGetValue(area.Worksheet, out var index))
                {
                    index = new RangeIndex();
                    indexes[area.Worksheet] = index;
                }

                index.Add(range);
            }

            if (range.Dependents.Add(cell))
            {
                cellRanges.Add(range);
            }
        }

        rangeDependencies[cell] = cellRanges;
    }
}

internal readonly record struct RangeArea(Worksheet Worksheet, int StartRow, int StartColumn, int EndRow, int EndColumn)
{
    public bool Contains(CellRef address) =>
        address.Row >= StartRow && address.Row <= EndRow && address.Column >= StartColumn && address.Column <= EndColumn;
}

internal sealed class RangeDependency(RangeArea area)
{
    public RangeArea Area { get; } = area;

    public HashSet<Cell> Dependents { get; } = [];
}

internal sealed class RangeIndex
{
    private const int BlockSize = 64;
    private const int MaxBlocks = 64;

    private readonly Dictionary<int, List<RangeDependency>> blocks = [];
    private readonly List<RangeDependency> tall = [];

    public void Add(RangeDependency range)
    {
        var (first, last) = Blocks(range.Area);

        if (last - first >= MaxBlocks)
        {
            tall.Add(range);
            return;
        }

        for (var block = first; block <= last; block++)
        {
            if (!blocks.TryGetValue(block, out var list))
            {
                list = [];
                blocks[block] = list;
            }

            list.Add(range);
        }
    }

    public void Remove(RangeDependency range)
    {
        var (first, last) = Blocks(range.Area);

        if (last - first >= MaxBlocks)
        {
            tall.Remove(range);
            return;
        }

        for (var block = first; block <= last; block++)
        {
            if (blocks.TryGetValue(block, out var list))
            {
                list.Remove(range);

                if (list.Count == 0)
                {
                    blocks.Remove(block);
                }
            }
        }
    }

    public IEnumerable<RangeDependency> Find(CellRef address)
    {
        if (blocks.TryGetValue(address.Row / BlockSize, out var list))
        {
            foreach (var range in list)
            {
                if (range.Area.Contains(address))
                {
                    yield return range;
                }
            }
        }

        foreach (var range in tall)
        {
            if (range.Area.Contains(address))
            {
                yield return range;
            }
        }
    }

    private static (int First, int Last) Blocks(RangeArea area) => (area.StartRow / BlockSize, area.EndRow / BlockSize);
}

class DependencyVisitor(Worksheet sheet) : IFormulaSyntaxNodeVisitor
{
    private readonly Worksheet sheet = sheet;

    public HashSet<Cell> Dependencies { get; } = [];

    public List<RangeArea> Ranges { get; } = [];

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
            Ranges.Add(new RangeArea(targetSheet, 0, Math.Min(startAddress.Column, endAddress.Column), Worksheet.MaxRows - 1, Math.Max(startAddress.Column, endAddress.Column)));
            return;
        }

        if (rangeSyntaxNode.Kind == RangeKind.Rows)
        {
            Ranges.Add(new RangeArea(targetSheet, Math.Min(startAddress.Row, endAddress.Row), 0, Math.Max(startAddress.Row, endAddress.Row), Worksheet.MaxColumns - 1));
            return;
        }

        Ranges.Add(new RangeArea(targetSheet, Math.Min(startAddress.Row, endAddress.Row), Math.Min(startAddress.Column, endAddress.Column), Math.Max(startAddress.Row, endAddress.Row), Math.Max(startAddress.Column, endAddress.Column)));
    }
}
