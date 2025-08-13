using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Represents a node in the Sankey diagram.
    /// </summary>
    internal class SankeyNode
    {
        /// <summary>
        /// Gets or sets the node identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the node label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the node value (optional).
        /// </summary>
        public double? Value { get; set; }
    }

    /// <summary>
    /// Represents a link between nodes in the Sankey diagram.
    /// </summary>
    internal class SankeyLink
    {
        /// <summary>
        /// Gets or sets the source node identifier.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the target node identifier.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the link value.
        /// </summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Represents a computed node with layout information.
    /// </summary>
    internal class ComputedSankeyNode : SankeyNode
    {
        /// <summary>
        /// Gets or sets the X position.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y position.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets the computed value.
        /// </summary>
        public double ComputedValue { get; set; }

        /// <summary>
        /// Gets or sets the layer index.
        /// </summary>
        public int Layer { get; set; }

        /// <summary>
        /// Gets or sets the incoming links.
        /// </summary>
        public List<ComputedSankeyLink> SourceLinks { get; set; } = new();

        /// <summary>
        /// Gets or sets the outgoing links.
        /// </summary>
        public List<ComputedSankeyLink> TargetLinks { get; set; } = new();

        /// <summary>
        /// Gets or sets the color index for CSS-based coloring.
        /// </summary>
        public int ColorIndex { get; set; }
    }

    /// <summary>
    /// Represents a computed link with path information.
    /// </summary>
    internal class ComputedSankeyLink : SankeyLink
    {
        /// <summary>
        /// Gets or sets the SVG path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the source node.
        /// </summary>
        public ComputedSankeyNode SourceNode { get; set; }

        /// <summary>
        /// Gets or sets the target node.
        /// </summary>
        public ComputedSankeyNode TargetNode { get; set; }

        /// <summary>
        /// Gets or sets the Y position at source.
        /// </summary>
        public double Y0 { get; set; }

        /// <summary>
        /// Gets or sets the Y position at target.
        /// </summary>
        public double Y1 { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// Gets or sets the width at the source node.
        /// </summary>
        public double WidthSource { get; set; }
        
        /// <summary>
        /// Gets or sets the width at the target node.
        /// </summary>
        public double WidthTarget { get; set; }
    }

    /// <summary>
    /// Defines the node alignment in Sankey diagram.
    /// </summary>
    public enum SankeyAlignment
    {
        /// <summary>
        /// Justify alignment.
        /// </summary>
        Justify,

        /// <summary>
        /// Left alignment.
        /// </summary>
        Left,

        /// <summary>
        /// Right alignment.
        /// </summary>
        Right,

        /// <summary>
        /// Center alignment.
        /// </summary>
        Center
    }
}