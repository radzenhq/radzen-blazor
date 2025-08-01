using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSankeyDiagram component.
    /// </summary>
    /// <typeparam name="TItem">The type of data item.</typeparam>
    public partial class RadzenSankeyDiagram<TItem> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the data. Each item represents a link/flow in the diagram.
        /// </summary>
        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which provides the source node ID.
        /// </summary>
        [Parameter]
        public string SourceProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which provides the target node ID.
        /// </summary>
        [Parameter]
        public string TargetProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which provides the flow value.
        /// </summary>
        [Parameter]
        public string ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the node labels. Key is the node ID, value is the label.
        /// </summary>
        [Parameter]
        public IDictionary<string, string> NodeLabels { get; set; }

        /// <summary>
        /// Gets or sets the node fill colors. If not specified, uses color scheme.
        /// </summary>
        [Parameter]
        public IList<string> NodeFills { get; set; }

        /// <summary>
        /// Gets or sets the link fill colors. If not specified, inherits from source node.
        /// </summary>
        [Parameter]
        public IList<string> LinkFills { get; set; }

        /// <summary>
        /// Gets or sets the color scheme of the chart.
        /// </summary>
        [Parameter]
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Pastel;

        /// <summary>
        /// Gets the actual width of the chart.
        /// </summary>
        protected double? Width { get; set; }

        /// <summary>
        /// Gets the actual height of the chart.
        /// </summary>
        protected double? Height { get; set; }

        /// <summary>
        /// Gets or sets the left margin.
        /// </summary>
        protected double MarginLeft { get; set; } = 80;

        /// <summary>
        /// Gets or sets the top margin.
        /// </summary>
        protected double MarginTop { get; set; } = 10;

        /// <summary>
        /// Gets or sets the right margin.
        /// </summary>
        protected double MarginRight { get; set; } = 80;

        /// <summary>
        /// Gets or sets the bottom margin.
        /// </summary>
        protected double MarginBottom { get; set; } = 10;

        /// <summary>
        /// Gets or sets the node width.
        /// </summary>
        [Parameter]
        public double NodeWidth { get; set; } = 24;

        /// <summary>
        /// Gets or sets the node padding.
        /// </summary>
        [Parameter]
        public double NodePadding { get; set; } = 8;

        /// <summary>
        /// Gets or sets the node alignment.
        /// </summary>
        [Parameter]
        public SankeyAlignment NodeAlignment { get; set; } = SankeyAlignment.Justify;

        // Node and link sort functions are internal implementation details
        internal Func<SankeyNode, SankeyNode, int> NodeSort { get; set; }
        internal Func<SankeyLink, SankeyLink, int> LinkSort { get; set; }

        /// <summary>
        /// Gets the computed nodes with layout.
        /// </summary>
        internal IList<ComputedSankeyNode> ComputedNodes { get; private set; }

        /// <summary>
        /// Gets the computed links with paths.
        /// </summary>
        internal IList<ComputedSankeyLink> ComputedLinks { get; private set; }

        // Property accessors
        private Func<TItem, string> sourceGetter;
        private Func<TItem, string> targetGetter;
        private Func<TItem, double> valueGetter;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var colorScheme = ColorScheme.ToString().ToLower();
            return $"rz-sankey-diagram rz-scheme-{colorScheme}";
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // Don't compute layout here - wait for JavaScript to provide dimensions
        }


        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool shouldUpdate = false;

            if (parameters.DidParameterChange(nameof(SourceProperty), SourceProperty))
            {
                var property = parameters.GetValueOrDefault<string>(nameof(SourceProperty));
                if (!string.IsNullOrEmpty(property))
                {
                    sourceGetter = PropertyAccess.Getter<TItem, string>(property);
                    shouldUpdate = true;
                }
            }
            else if (sourceGetter == null && !string.IsNullOrEmpty(SourceProperty))
            {
                sourceGetter = PropertyAccess.Getter<TItem, string>(SourceProperty);
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(TargetProperty), TargetProperty))
            {
                var property = parameters.GetValueOrDefault<string>(nameof(TargetProperty));
                if (!string.IsNullOrEmpty(property))
                {
                    targetGetter = PropertyAccess.Getter<TItem, string>(property);
                    shouldUpdate = true;
                }
            }
            else if (targetGetter == null && !string.IsNullOrEmpty(TargetProperty))
            {
                targetGetter = PropertyAccess.Getter<TItem, string>(TargetProperty);
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(ValueProperty), ValueProperty))
            {
                var property = parameters.GetValueOrDefault<string>(nameof(ValueProperty));
                if (!string.IsNullOrEmpty(property))
                {
                    valueGetter = PropertyAccess.Getter<TItem, double>(property);
                    shouldUpdate = true;
                }
            }
            else if (valueGetter == null && !string.IsNullOrEmpty(ValueProperty))
            {
                valueGetter = PropertyAccess.Getter<TItem, double>(ValueProperty);
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(Data), Data) || 
                parameters.DidParameterChange(nameof(NodeWidth), NodeWidth) ||
                parameters.DidParameterChange(nameof(NodePadding), NodePadding) ||
                parameters.DidParameterChange(nameof(NodeAlignment), NodeAlignment) ||
                parameters.DidParameterChange(nameof(ColorScheme), ColorScheme) ||
                parameters.DidParameterChange(nameof(NodeFills), NodeFills) ||
                parameters.DidParameterChange(nameof(LinkFills), LinkFills) ||
                parameters.DidParameterChange(nameof(NodeLabels), NodeLabels) ||
                shouldUpdate)
            {
                shouldUpdate = true;
            }

            await base.SetParametersAsync(parameters);

            if (shouldUpdate)
            {
                ComputeLayout();
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!Visible)
            {
                return;
            }

            if (firstRender || !Width.HasValue || !Height.HasValue)
            {
                var rect = await JSRuntime.InvokeAsync<Rect>("Radzen.createResizable", Element, Reference);
                
                if (!Width.HasValue && rect.Width > 0)
                {
                    Width = rect.Width;
                }
                else if (!Width.HasValue)
                {
                    // Fallback if JavaScript sizing fails
                    Width = 800;
                }

                if (!Height.HasValue && rect.Height > 0)
                {
                    Height = rect.Height;
                }
                else if (!Height.HasValue)
                {
                    // Fallback if JavaScript sizing fails
                    Height = 400;
                }

                if (Width.HasValue && Height.HasValue)
                {
                    ComputeLayout();
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Called by JavaScript when the chart container is resized.
        /// </summary>
        [JSInvokable]
        public async Task Resize(double width, double height)
        {
            var stateHasChanged = false;

            if (Width != width)
            {
                Width = width;
                stateHasChanged = true;
            }

            if (Height != height)
            {
                Height = height;
                stateHasChanged = true;
            }

            if (stateHasChanged)
            {
                ComputeLayout();
                await InvokeAsync(StateHasChanged);
            }
        }

        private void ComputeLayout()
        {
            if (Data == null || !Width.HasValue || !Height.HasValue || Width <= 0 || Height <= 0)
            {
                ComputedNodes = null;
                ComputedLinks = null;
                return;
            }

            // Validate required properties
            if (string.IsNullOrEmpty(SourceProperty) || string.IsNullOrEmpty(TargetProperty) || string.IsNullOrEmpty(ValueProperty))
            {
                ComputedNodes = null;
                ComputedLinks = null;
                return;
            }

            if (sourceGetter == null || targetGetter == null || valueGetter == null)
            {
                ComputedNodes = null;
                ComputedLinks = null;
                return;
            }

            try
            {
                // Extract nodes and links from data
                var nodeMap = new Dictionary<string, SankeyNode>();
                var sankeyLinks = new List<SankeyLink>();
                
                foreach (var item in Data)
                {
                    var source = sourceGetter(item);
                    var target = targetGetter(item);
                    var value = valueGetter(item);
                    
                    // Create or update source node
                    if (!nodeMap.ContainsKey(source))
                    {
                        nodeMap[source] = new SankeyNode 
                        { 
                            Id = source,
                            Label = NodeLabels?.ContainsKey(source) == true ? NodeLabels[source] : source
                        };
                    }
                    
                    // Create or update target node
                    if (!nodeMap.ContainsKey(target))
                    {
                        nodeMap[target] = new SankeyNode 
                        { 
                            Id = target,
                            Label = NodeLabels?.ContainsKey(target) == true ? NodeLabels[target] : target
                        };
                    }
                    
                    // Create link
                    sankeyLinks.Add(new SankeyLink
                    {
                        Source = source,
                        Target = target,
                        Value = value
                    });
                }
                
                var sankeyNodes = nodeMap.Values.ToList();

                var layoutWidth = Width.Value - MarginLeft - MarginRight;
                var layoutHeight = Height.Value - MarginTop - MarginBottom;
                
                // Ensure positive dimensions
                layoutWidth = Math.Max(100, layoutWidth);
                layoutHeight = Math.Max(100, layoutHeight);
                
                var layout = new SankeyLayout
                {
                    Width = layoutWidth,
                    Height = layoutHeight,
                    NodeWidth = NodeWidth,
                    NodePadding = NodePadding,
                    NodeAlignment = NodeAlignment,
                    NodeSort = NodeSort,
                    LinkSort = LinkSort
                };

                (ComputedNodes, ComputedLinks) = layout.Compute(sankeyNodes, sankeyLinks);
                
                // Assign colors to nodes
                if (ComputedNodes != null)
                {
                    for (int i = 0; i < ComputedNodes.Count; i++)
                    {
                        var node = ComputedNodes[i];
                        
                        // Use explicit color if provided, otherwise use color scheme
                        if (NodeFills != null && i < NodeFills.Count)
                        {
                            node.ColorIndex = -1; // Indicates explicit color
                        }
                        else
                        {
                            node.ColorIndex = i;
                        }
                    }
                }
            }
            catch (Exception)
            {
                ComputedNodes = null;
                ComputedLinks = null;
            }
        }

        /// <summary>
        /// Gets the fill color for a node.
        /// </summary>
        internal string GetNodeFill(ComputedSankeyNode node)
        {
            if (NodeFills != null)
            {
                var index = ComputedNodes.IndexOf(node);
                if (index >= 0 && index < NodeFills.Count)
                {
                    return NodeFills[index];
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the fill color for a link.
        /// </summary>
        internal string GetLinkFill(ComputedSankeyLink link)
        {
            if (LinkFills != null)
            {
                var index = ComputedLinks.IndexOf(link);
                if (index >= 0 && index < LinkFills.Count)
                {
                    return LinkFills[index];
                }
            }
            return null;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (IsJSRuntimeAvailable)
            {
                try
                {
                    JSRuntime.InvokeVoidAsync("Radzen.destroyResizable", Element);
                }
                catch (Exception)
                {
                    // Ignore errors during disposal
                }
            }

            base.Dispose();
        }

        class Rect
        {
            public double Width { get; set; }
            public double Height { get; set; }
        }
    }
}