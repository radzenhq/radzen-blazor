using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// Maps every block below a semi-transparent container to the product of its ancestor
// container opacities. First-class container boxes (section-level, band-level and
// nested) resolve their decoration opacity through ContainerOpacity; CellOpacity
// recovers the value through a cell's child blocks for real table cells under
// translucent containers.
internal sealed class OpacityResolver
{
    private readonly Dictionary<Block, double> byBlock = [];
    private readonly Walker walker;

    public OpacityResolver(DocumentBuilder builder)
    {
        walker = new Walker(this);
        foreach (var section in builder.Sections)
        {
            Walk(section.Blocks, 1);
            Walk(section.Header.Blocks, 1);
            Walk(section.Footer.Blocks, 1);
        }
    }

    // The decoration opacity of a FIRST-CLASS container box (section-level or nested):
    // the container's own opacity times the product of its ancestor container opacities.
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

            block.Accept(walker, opacity);
        }
    }

    // Only containers and tables carry descendants that inherit the ambient opacity; every
    // other block kind has none to recurse into (the identity Default).
    private sealed class Walker(OpacityResolver owner) : BlockVisitor<double, Nothing>
    {
        protected override Nothing Default(Block block, double opacity) => default;

        public override Nothing Visit(Container container, double opacity)
        {
            owner.Walk(container.Blocks, opacity * container.Opacity);
            return default;
        }

        public override Nothing Visit(Table table, double opacity)
        {
            foreach (var row in table.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    owner.Walk(cell.Blocks, opacity);
                }
            }

            return default;
        }
    }
}
