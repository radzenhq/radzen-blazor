using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// Computes the layout for a Sankey diagram.
    /// </summary>
    internal class SankeyLayout
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double NodeWidth { get; set; } = 24;
        public double NodePadding { get; set; } = 8;
        public SankeyAlignment NodeAlignment { get; set; } = SankeyAlignment.Justify;
        public Func<SankeyNode, SankeyNode, int> NodeSort { get; set; }
        public Func<SankeyLink, SankeyLink, int> LinkSort { get; set; }

        private const int MaxIterations = 32;

        public (IList<ComputedSankeyNode>, IList<ComputedSankeyLink>) Compute(IEnumerable<SankeyNode> nodes, IEnumerable<SankeyLink> links)
        {
            // Create computed nodes
            var computedNodes = nodes.Select(n => new ComputedSankeyNode
            {
                Id = n.Id,
                Label = n.Label,
                Value = n.Value,
                Width = NodeWidth,
                Y = 0,  // Initialize Y to 0
                Height = 0  // Initialize Height
            }).ToList();

            // Create node lookup
            var nodeById = computedNodes.ToDictionary(n => n.Id);

            // Create computed links
            var computedLinks = links.Select(l => new ComputedSankeyLink
            {
                Source = l.Source,
                Target = l.Target,
                Value = l.Value,
                SourceNode = nodeById[l.Source],
                TargetNode = nodeById[l.Target]
            }).ToList();

            // Connect nodes and links
            foreach (var link in computedLinks)
            {
                link.SourceNode.SourceLinks.Add(link);
                link.TargetNode.TargetLinks.Add(link);
            }

            // Compute node values
            ComputeNodeValues(computedNodes);

            // Compute node layers
            ComputeNodeLayers(computedNodes);

            // Compute node breadths
            ComputeNodeBreadths(computedNodes);

            // Compute node depths
            ComputeNodeDepths(computedNodes);

            // Compute link paths
            ComputeLinkPaths(computedLinks);

            return (computedNodes, computedLinks);
        }

        private void ComputeNodeValues(List<ComputedSankeyNode> nodes)
        {
            foreach (var node in nodes)
            {
                // Calculate and store the actual sums
                var incomingSum = node.TargetLinks.Sum(l => l.Value);
                var outgoingSum = node.SourceLinks.Sum(l => l.Value);
                
                if (node.Value.HasValue)
                {
                    node.ComputedValue = node.Value.Value;
                }
                else
                {
                    node.ComputedValue = Math.Max(incomingSum, outgoingSum);
                }
            }
        }

        private void ComputeNodeLayers(List<ComputedSankeyNode> nodes)
        {
            // Find nodes with no incoming links
            var sources = nodes.Where(n => !n.TargetLinks.Any()).ToList();
            var visited = new HashSet<ComputedSankeyNode>();
            var layers = new List<List<ComputedSankeyNode>>();

            // Breadth-first traversal
            var queue = new Queue<ComputedSankeyNode>(sources);
            foreach (var source in sources)
            {
                source.Layer = 0;
                visited.Add(source);
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                
                foreach (var link in node.SourceLinks)
                {
                    var target = link.TargetNode;
                    if (!visited.Contains(target))
                    {
                        target.Layer = node.Layer + 1;
                        visited.Add(target);
                        queue.Enqueue(target);
                    }
                    else
                    {
                        target.Layer = Math.Max(target.Layer, node.Layer + 1);
                    }
                }
            }

            // Handle cycles and disconnected nodes
            foreach (var node in nodes)
            {
                if (!visited.Contains(node))
                {
                    node.Layer = 0;
                }
            }
        }

        private void ComputeNodeBreadths(List<ComputedSankeyNode> nodes)
        {
            var maxLayer = nodes.Max(n => n.Layer);
            
            // Group nodes by layer
            var nodesByLayer = nodes.GroupBy(n => n.Layer).OrderBy(g => g.Key).ToList();

            foreach (var layer in nodesByLayer)
            {
                var layerNodes = layer.ToList();

                // Sort nodes in layer
                if (NodeSort != null)
                {
                    layerNodes.Sort((a, b) => NodeSort(a, b));
                }

                // Set X positions
                foreach (var node in layerNodes)
                {
                    if (maxLayer == 0)
                    {
                        // Single layer - center it
                        node.X = (Width - NodeWidth) / 2;
                    }
                    else
                    {
                        var layerSpacing = (Width - NodeWidth) / maxLayer;
                        
                        switch (NodeAlignment)
                        {
                            case SankeyAlignment.Left:
                                // Align nodes to the left with compact spacing
                                var leftSpacing = Math.Min(layerSpacing, (Width * 0.7) / maxLayer);
                                node.X = node.Layer * leftSpacing;
                                break;
                            case SankeyAlignment.Right:
                                // Align nodes to the right with compact spacing
                                var rightSpacing = Math.Min(layerSpacing, (Width * 0.7) / maxLayer);
                                var totalRightWidth = maxLayer * rightSpacing;
                                node.X = Width - NodeWidth - totalRightWidth + (node.Layer * rightSpacing);
                                break;
                            case SankeyAlignment.Center:
                                // Center with proportional spacing
                                var centerSpacing = layerSpacing * 0.8; // Slightly compressed
                                var totalWidth = maxLayer * centerSpacing;
                                var startX = (Width - totalWidth - NodeWidth) / 2;
                                node.X = startX + node.Layer * centerSpacing;
                                break;
                            case SankeyAlignment.Justify:
                            default:
                                // Distribute evenly across full width
                                node.X = node.Layer * layerSpacing;
                                break;
                        }
                    }
                }
            }
        }

        private void ComputeNodeDepths(List<ComputedSankeyNode> nodes)
        {
            // Group nodes by layer
            var nodesByLayer = nodes.GroupBy(n => n.Layer).OrderBy(g => g.Key).ToList();

            // Initialize node depths
            foreach (var layer in nodesByLayer)
            {
                var layerNodes = layer.ToList();
                InitializeNodeDepths(layerNodes);
            }

            // Resolve collisions
            ResolveCollisions(nodesByLayer);

            // Sort links
            foreach (var node in nodes)
            {
                if (LinkSort != null)
                {
                    node.SourceLinks.Sort((a, b) => LinkSort(a, b));
                    node.TargetLinks.Sort((a, b) => LinkSort(a, b));
                }
            }

            // Assign link positions
            AssignLinkDepths(nodes);
        }

        private void InitializeNodeDepths(List<ComputedSankeyNode> nodes)
        {
            if (nodes.Count == 0) return;
            
            var totalValue = nodes.Sum(n => n.ComputedValue);
            var totalPadding = Math.Max(0, (nodes.Count - 1) * NodePadding);
            var availableHeight = Math.Max(1, Height - totalPadding);
            
            if (totalValue > 0)
            {
                var valueScale = availableHeight / totalValue;
                var y = 0.0;

                foreach (var node in nodes)
                {
                    // Ensure minimum height of 3px for visibility
                    node.Height = Math.Max(3, node.ComputedValue * valueScale);
                    node.Y = y;
                    y += node.Height + NodePadding;
                }
            }
            else
            {
                // Distribute evenly if no values
                var nodeHeight = Math.Max(1, availableHeight / nodes.Count);
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Height = nodeHeight;
                    nodes[i].Y = i * (nodeHeight + NodePadding);
                }
            }
        }

        private void ResolveCollisions(List<IGrouping<int, ComputedSankeyNode>> nodesByLayer)
        {
            // Iteratively relax node positions
            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                var alpha = 1 - Math.Pow(0.001, 1.0 / MaxIterations);
                
                // Forward pass
                for (int i = 1; i < nodesByLayer.Count; i++)
                {
                    RelaxLeftToRight(nodesByLayer[i].ToList(), alpha);
                }

                // Backward pass
                for (int i = nodesByLayer.Count - 2; i >= 0; i--)
                {
                    RelaxRightToLeft(nodesByLayer[i].ToList(), alpha);
                }
            }
        }

        private void RelaxLeftToRight(List<ComputedSankeyNode> nodes, double alpha)
        {
            foreach (var node in nodes)
            {
                if (node.TargetLinks.Count > 0)
                {
                    var y = node.TargetLinks.Sum(l => (l.SourceNode.Y + l.Y0) * l.Value) / node.ComputedValue;
                    node.Y += (y - node.Y) * alpha;
                }
            }

            // Resolve overlaps
            nodes = nodes.OrderBy(n => n.Y).ToList();
            var y0 = 0.0;
            foreach (var node in nodes)
            {
                var dy = y0 - node.Y;
                if (dy > 0)
                {
                    node.Y += dy;
                }
                y0 = node.Y + node.Height + NodePadding;
            }

            // Adjust to fit within height
            if (nodes.Count > 0)
            {
                // Find min and max Y positions
                var minY = nodes.Min(n => n.Y);
                var maxY = nodes.Max(n => n.Y + n.Height);
                var totalHeight = maxY - minY;
                
                // First, normalize to start at 0
                if (minY != 0)
                {
                    foreach (var node in nodes)
                    {
                        node.Y -= minY;
                    }
                    maxY -= minY;
                    minY = 0;
                }
                
                // Then scale or center
                if (totalHeight > Height)
                {
                    // Scale down to fit
                    var scale = Height / totalHeight;
                    foreach (var node in nodes)
                    {
                        node.Y *= scale;
                        node.Height *= scale;
                    }
                }
                else
                {
                    // Center vertically - but keep within bounds
                    var offset = Math.Min((Height - totalHeight) / 2, Height * 0.1); // Max 10% of height as offset
                    foreach (var node in nodes)
                    {
                        node.Y += offset;
                    }
                }
            }
        }

        private void RelaxRightToLeft(List<ComputedSankeyNode> nodes, double alpha)
        {
            foreach (var node in nodes)
            {
                if (node.SourceLinks.Count > 0)
                {
                    var y = node.SourceLinks.Sum(l => (l.TargetNode.Y + l.Y1) * l.Value) / node.ComputedValue;
                    node.Y += (y - node.Y) * alpha;
                }
            }

            // Resolve overlaps
            nodes = nodes.OrderBy(n => n.Y).ToList();
            var y0 = 0.0;
            foreach (var node in nodes)
            {
                var dy = y0 - node.Y;
                if (dy > 0)
                {
                    node.Y += dy;
                }
                y0 = node.Y + node.Height + NodePadding;
            }

            // Adjust to fit within height
            if (nodes.Count > 0)
            {
                // Find min and max Y positions
                var minY = nodes.Min(n => n.Y);
                var maxY = nodes.Max(n => n.Y + n.Height);
                var totalHeight = maxY - minY;
                
                // First, normalize to start at 0
                if (minY != 0)
                {
                    foreach (var node in nodes)
                    {
                        node.Y -= minY;
                    }
                    maxY -= minY;
                    minY = 0;
                }
                
                // Then scale or center
                if (totalHeight > Height)
                {
                    // Scale down to fit
                    var scale = Height / totalHeight;
                    foreach (var node in nodes)
                    {
                        node.Y *= scale;
                        node.Height *= scale;
                    }
                }
                else
                {
                    // Center vertically - but keep within bounds
                    var offset = Math.Min((Height - totalHeight) / 2, Height * 0.1); // Max 10% of height as offset
                    foreach (var node in nodes)
                    {
                        node.Y += offset;
                    }
                }
            }
        }

        private void AssignLinkDepths(List<ComputedSankeyNode> nodes)
        {
            // Process all nodes to set source widths and Y0 positions
            foreach (var node in nodes)
            {
                var outgoingSum = node.SourceLinks.Sum(l => l.Value);
                if (outgoingSum > 0)
                {
                    var y0 = 0.0;
                    var sortedSourceLinks = node.SourceLinks.OrderBy(l => l.TargetNode.Y).ToList();
                    foreach (var link in sortedSourceLinks)
                    {
                        link.Y0 = y0;
                        link.WidthSource = (link.Value / outgoingSum) * node.Height;
                        link.Width = link.WidthSource; // Default width
                        y0 += link.WidthSource;
                    }
                }
            }
            
            // Process all nodes to set target widths and Y1 positions
            foreach (var node in nodes)
            {
                var incomingSum = node.TargetLinks.Sum(l => l.Value);
                if (incomingSum > 0)
                {
                    var y1 = 0.0;
                    var sortedTargetLinks = node.TargetLinks.OrderBy(l => l.SourceNode.Y).ToList();
                    foreach (var link in sortedTargetLinks)
                    {
                        link.Y1 = y1;
                        link.WidthTarget = (link.Value / incomingSum) * node.Height;
                        y1 += link.WidthTarget;
                    }
                }
            }
        }

        private void ComputeLinkPaths(List<ComputedSankeyLink> links)
        {
            foreach (var link in links)
            {
                var x0 = link.SourceNode.X + link.SourceNode.Width;
                var x1 = link.TargetNode.X;
                var y0 = link.SourceNode.Y + link.Y0;
                var y1 = link.TargetNode.Y + link.Y1;
                var widthSource = link.WidthSource > 0 ? link.WidthSource : link.Width;
                var widthTarget = link.WidthTarget > 0 ? link.WidthTarget : link.Width;

                // Create a smooth curve using cubic bezier that transitions between different widths
                var xi = (x0 + x1) / 2;

                link.Path = $"M{x0.ToInvariantString()},{y0.ToInvariantString()} " +
                           $"C{xi.ToInvariantString()},{y0.ToInvariantString()} " +
                           $"{xi.ToInvariantString()},{y1.ToInvariantString()} " +
                           $"{x1.ToInvariantString()},{y1.ToInvariantString()} " +
                           $"L{x1.ToInvariantString()},{(y1 + widthTarget).ToInvariantString()} " +
                           $"C{xi.ToInvariantString()},{(y1 + widthTarget).ToInvariantString()} " +
                           $"{xi.ToInvariantString()},{(y0 + widthSource).ToInvariantString()} " +
                           $"{x0.ToInvariantString()},{(y0 + widthSource).ToInvariantString()} " +
                           $"Z";
            }
        }
    }
}