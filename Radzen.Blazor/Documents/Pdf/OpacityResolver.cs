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
            Walk(section.Blocks, 1);
            Walk(section.Header.Blocks, 1);
            Walk(section.Footer.Blocks, 1);
        }
    }

    // The decoration opacity of a FIRST-CLASS container box: the container's own
    // opacity times the product of its ancestor container opacities. CellOpacity
    // stays for the still-lowered (nested/overlay/rotated) path, which recovers
    // the value through the synthetic cell's child blocks.
    public double ContainerOpacity(Container container)
        => (byBlock.TryGetValue(container, out var inherited) ? inherited : 1) * container.Opacity;

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

    // Every block below a semi-transparent container maps to the product of its
    // ancestor container opacities, so nested containers and tables inherit it.
    private void Walk(BlockCollection blocks, double opacity)
    {
        foreach (var block in blocks)
        {
            if (opacity < 1)
            {
                byBlock[block] = opacity;
            }

            if (block is Container container)
            {
                Walk(container.Blocks, opacity * container.Opacity);
            }
            else if (block is Table table)
            {
                foreach (var row in table.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        Walk(cell.Blocks, opacity);
                    }
                }
            }
        }
    }
}
