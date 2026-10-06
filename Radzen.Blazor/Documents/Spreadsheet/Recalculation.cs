using System.Collections.Generic;

#nullable enable

namespace Radzen.Documents.Spreadsheet;

internal sealed class Recalculation
{
    private readonly HashSet<Cell> pending = [];
    private readonly List<Cell> order = [];
    private readonly HashSet<Cell> waiting = [];
    private readonly Dictionary<Cell, HashSet<Cell>> circular = [];
    private readonly Dictionary<RangeKey, List<CellData>> rangeValues = [];

    public Recalculation(IEnumerable<Cell> cells)
    {
        foreach (var cell in cells)
        {
            if (cell.FormulaSyntaxTree is not null && pending.Add(cell))
            {
                order.Add(cell);
            }
        }
    }

    public bool IsPending(Cell cell) => pending.Contains(cell);

    public bool IsCircular(Cell cell, Cell precedent) => circular.TryGetValue(cell, out var precedents) && precedents.Contains(precedent);

    public bool TryGetRangeValues(RangeKey key, out List<CellData> values) => rangeValues.TryGetValue(key, out values!);

    public void SetRangeValues(RangeKey key, List<CellData> values) => rangeValues[key] = values;

    public void Run()
    {
        var stack = new Stack<Cell>();

        foreach (var root in order)
        {
            if (!pending.Contains(root))
            {
                continue;
            }

            stack.Push(root);

            while (stack.TryPeek(out var cell))
            {
                if (!pending.Contains(cell))
                {
                    stack.Pop();
                    continue;
                }

                var missing = cell.Worksheet.EvaluateFormula(cell, this);

                if (missing.Count == 0)
                {
                    pending.Remove(cell);
                    waiting.Remove(cell);
                    circular.Remove(cell);
                    stack.Pop();
                    continue;
                }

                waiting.Add(cell);

                foreach (var precedent in missing)
                {
                    if (waiting.Contains(precedent))
                    {
                        if (!circular.TryGetValue(cell, out var precedents))
                        {
                            precedents = [];
                            circular[cell] = precedents;
                        }

                        precedents.Add(precedent);
                    }
                    else
                    {
                        stack.Push(precedent);
                    }
                }
            }
        }
    }
}
