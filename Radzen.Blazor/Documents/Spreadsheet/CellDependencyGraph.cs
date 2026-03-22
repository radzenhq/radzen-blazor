using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

internal class CellDependencyGraph
{
    private readonly Dictionary<Cell, HashSet<Cell>> dependencies = [];
    private readonly Dictionary<Cell, HashSet<Cell>> dependents = [];

    private IEnumerable<Cell> GetDependentCells(Cell cell)
    {
        if (dependents.TryGetValue(cell, out var cells))
        {
            return cells;
        }
        return [];
    }

    public IEnumerable<Cell> GetTopologicallySortedDependencies(Cell cell) => GetTopologicallySortedDependencies(GetDependentCells(cell));

    public IEnumerable<Cell> GetTopologicallySortedDependencies() => GetTopologicallySortedDependencies(dependencies.Keys);

    private List<Cell> GetTopologicallySortedDependencies(IEnumerable<Cell> cells)
    {
        var visited = new HashSet<Cell>();
        var result = new List<Cell>();

        void Visit(Cell cell)
        {
            if (!visited.Add(cell))
            {
                return;
            }

            foreach (var dependentCell in GetDependentCells(cell))
            {
                Visit(dependentCell);
            }
            result.Add(cell);
        }

        foreach (var cell in cells)
        {
            if (!visited.Contains(cell))
            {
                Visit(cell);
            }
        }

        result.Reverse();

        return result;
    }

    public void Add(Cell cell)
    {
        if (cell.Formula == null)
        {
            return;
        }

        var tree = cell.FormulaSyntaxTree ?? FormulaParser.Parse(cell.Formula);
        var visitor = new DependencyVisitor(cell.Worksheet);
        tree.Root.Accept(visitor);

        if (dependencies.TryGetValue(cell, out var oldDependencies))
        {
            foreach (var dependency in oldDependencies)
            {
                if (dependents.TryGetValue(dependency, out var dependentCells))
                {
                    dependentCells.Remove(cell);
                }
            }
        }

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
    }

}

class DependencyVisitor(Worksheet sheet) : IFormulaSyntaxNodeVisitor
{
    private readonly Worksheet sheet = sheet;

    public HashSet<Cell> Dependencies { get; } = [];

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

        return target?.Name == worksheetName ? target : null;
    }

    public void VisitCell(CellSyntaxNode cellIdentifierSyntaxNode)
    {
        var address = cellIdentifierSyntaxNode.Token.Address;
        var targetSheet = ResolveSheet(address.Worksheet);

        if (targetSheet == null)
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

        if (targetSheet == null)
        {
            return;
        }

        var startRow = Math.Min(startAddress.Row, endAddress.Row);
        var endRow = Math.Max(startAddress.Row, endAddress.Row);
        var startCol = Math.Min(startAddress.Column, endAddress.Column);
        var endCol = Math.Max(startAddress.Column, endAddress.Column);

        for (var row = startRow; row <= endRow; row++)
        {
            for (var col = startCol; col <= endCol; col++)
            {
                if (row >= targetSheet.RowCount || col >= targetSheet.ColumnCount)
                {
                    continue;
                }

                Dependencies.Add(targetSheet.Cells[new CellRef(row, col)]);
            }
        }
    }
}
