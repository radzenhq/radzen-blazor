using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// Containers lower onto the table engine as synthetic single-cell tables
// (Paginator.LowerContainer) whose cell shares the container's child Block
// instances. Mapping each child to its container's opacity lets TableEmitter
// recover the opacity of a lowered container's box decoration from the cell.
internal sealed class OpacityResolver
{
    private readonly Dictionary<Block, double> byBlock = [];

    public OpacityResolver(DocumentBuilder builder)
    {
        foreach (var section in builder.Sections)
        {
            Walk(section.Blocks);
            Walk(section.Header.Blocks);
            Walk(section.Footer.Blocks);
        }
    }

    public double CellOpacity(Cell cell)
    {
        foreach (var block in cell.Blocks)
        {
            if (byBlock.TryGetValue(block, out var opacity))
            {
                return opacity;
            }
        }

        return 1;
    }

    private void Walk(BlockCollection blocks)
    {
        foreach (var block in blocks)
        {
            if (block is Container container)
            {
                if (container.Opacity < 1)
                {
                    foreach (var child in container.Blocks)
                    {
                        byBlock[child] = container.Opacity;
                    }
                }

                Walk(container.Blocks);
            }
            else if (block is Table table)
            {
                foreach (var row in table.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        Walk(cell.Blocks);
                    }
                }
            }
        }
    }
}
