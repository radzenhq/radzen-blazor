using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

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

        var node = FormulaParser.Parse(cell.Formula);
        var visitor = new DependencyVisitor(cell.Sheet);
        node.Accept(visitor);

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

class DependencyVisitor(Sheet sheet) : IFormulaSyntaxNodeVisitor
{
    private readonly Sheet sheet = sheet;

    public HashSet<Cell> Dependencies { get; } = [];

    public void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
    {
    }

    public void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        binaryExpressionSyntaxNode.Left.Accept(this);
        binaryExpressionSyntaxNode.Right.Accept(this);
    }

    public void VisitCell(CellSyntaxNode cellIdentifierSyntaxNode)
    {
        var address = cellIdentifierSyntaxNode.Token.AddressValue;
        if (address.Row >= sheet.RowCount || address.Column >= sheet.ColumnCount)
        {
            // Out of bounds, do not add dependency
            return;
        }
        var cell = sheet.Cells[address];
        Dependencies.Add(cell);
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
        rangeSyntaxNode.Start.Accept(this);
        rangeSyntaxNode.End.Accept(this);
    }
}