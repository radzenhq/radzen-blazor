using System.Collections.Generic;

namespace Radzen.Documents.Layout;

internal sealed class OpacityResolver
{
    public static readonly OpacityResolver None = new();

    private readonly Dictionary<Block, double> byBlock = [];
    private readonly Walker walker;

    private OpacityResolver() => walker = new Walker(this);

    public OpacityResolver(Radzen.Documents.Document builder)
    {
        walker = new Walker(this);
        foreach (var section in builder.Sections)
        {
            Walk(section.Blocks, 1);
            Walk(section.Header.Blocks, 1);
            Walk(section.Footer.Blocks, 1);
        }
    }

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

        public override Nothing Visit(List list, double opacity)
        {
            foreach (var item in list.Items)
            {
                owner.Walk(item.Blocks, opacity);
            }

            return default;
        }
    }
}
