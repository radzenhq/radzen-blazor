using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSankeyDiagram component.
    /// </summary>
    /// <typeparam name="TItem">The type of data item.</typeparam>
    public partial class RadzenSankeyDiagram<TItem> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the tooltip service.
        /// </summary>
        [Inject]
        public TooltipService TooltipService { get; set; }
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
        /// Specifies the property of <typeparamref name="TItem" /> which provides the source node label.
        /// </summary>
        [Parameter]
        public string SourceLabelProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which provides the target node label.
        /// </summary>
        [Parameter]
        public string TargetLabelProperty { get; set; }

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
        private double? Width { get; set; }

        /// <summary>
        /// Gets the actual height of the chart.
        /// </summary>
        private double? Height { get; set; }

        /// <summary>
        /// Gets or sets the left margin.
        /// </summary>
        private double MarginLeft { get; set; } = 80;

        /// <summary>
        /// Gets or sets the top margin.
        /// </summary>
        private double MarginTop { get; set; } = 10;

        /// <summary>
        /// Gets or sets the right margin.
        /// </summary>
        private double MarginRight { get; set; } = 80;

        /// <summary>
        /// Gets or sets the bottom margin.
        /// </summary>
        private double MarginBottom { get; set; } = 10;

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

        /// <summary>
        /// Gets or sets the value formatter for tooltip display.
        /// </summary>
        [Parameter]
        public Func<double, string> ValueFormatter { get; set; }

        /// <summary>
        /// Gets or sets the tooltip text for "Value".
        /// </summary>
        [Parameter]
        public string ValueText { get; set; } = "Value";

        /// <summary>
        /// Gets or sets the tooltip text for "Incoming".
        /// </summary>
        [Parameter]
        public string IncomingText { get; set; } = "Incoming";

        /// <summary>
        /// Gets or sets the tooltip text for "Outgoing".
        /// </summary>
        [Parameter]
        public string OutgoingText { get; set; } = "Outgoing";

        /// <summary>
        /// Gets or sets the tooltip text for "Flow".
        /// </summary>
        [Parameter]
        public string FlowText { get; set; } = "Flow";

        /// <summary>
        /// Gets or sets the CSS style of the tooltip.
        /// </summary>
        [Parameter]
        public string TooltipStyle { get; set; }

        /// <summary>
        /// Gets or sets whether to animate the flow in the links.
        /// </summary>
        [Parameter]
        public bool Animated { get; set; }

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
        private Func<TItem, string> sourceLabelGetter;
        private Func<TItem, string> targetLabelGetter;

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

            if (parameters.DidParameterChange(nameof(SourceLabelProperty), SourceLabelProperty))
            {
                var property = parameters.GetValueOrDefault<string>(nameof(SourceLabelProperty));
                if (!string.IsNullOrEmpty(property))
                {
                    sourceLabelGetter = PropertyAccess.Getter<TItem, string>(property);
                }
                else
                {
                    sourceLabelGetter = null;
                }
                shouldUpdate = true;
            }
            else if (sourceLabelGetter == null && !string.IsNullOrEmpty(SourceLabelProperty))
            {
                sourceLabelGetter = PropertyAccess.Getter<TItem, string>(SourceLabelProperty);
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(TargetLabelProperty), TargetLabelProperty))
            {
                var property = parameters.GetValueOrDefault<string>(nameof(TargetLabelProperty));
                if (!string.IsNullOrEmpty(property))
                {
                    targetLabelGetter = PropertyAccess.Getter<TItem, string>(property);
                }
                else
                {
                    targetLabelGetter = null;
                }
                shouldUpdate = true;
            }
            else if (targetLabelGetter == null && !string.IsNullOrEmpty(TargetLabelProperty))
            {
                targetLabelGetter = PropertyAccess.Getter<TItem, string>(TargetLabelProperty);
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(Data), Data) || 
                parameters.DidParameterChange(nameof(NodeWidth), NodeWidth) ||
                parameters.DidParameterChange(nameof(NodePadding), NodePadding) ||
                parameters.DidParameterChange(nameof(NodeAlignment), NodeAlignment) ||
                parameters.DidParameterChange(nameof(ColorScheme), ColorScheme) ||
                parameters.DidParameterChange(nameof(NodeFills), NodeFills) ||
                parameters.DidParameterChange(nameof(LinkFills), LinkFills) ||
                parameters.DidParameterChange(nameof(SourceLabelProperty), SourceLabelProperty) ||
                parameters.DidParameterChange(nameof(TargetLabelProperty), TargetLabelProperty) ||
                parameters.DidParameterChange(nameof(Animated), Animated) ||
                shouldUpdate)
            {
                shouldUpdate = true;
            }

            // Check for tooltip-related parameter changes that don't require layout recomputation
            var tooltipParametersChanged = 
                parameters.DidParameterChange(nameof(ValueFormatter), ValueFormatter) ||
                parameters.DidParameterChange(nameof(ValueText), ValueText) ||
                parameters.DidParameterChange(nameof(IncomingText), IncomingText) ||
                parameters.DidParameterChange(nameof(OutgoingText), OutgoingText) ||
                parameters.DidParameterChange(nameof(FlowText), FlowText) ||
                parameters.DidParameterChange(nameof(TooltipStyle), TooltipStyle);

            await base.SetParametersAsync(parameters);

            if (shouldUpdate)
            {
                ComputeLayout();
            }
            else if (tooltipParametersChanged)
            {
                // Just trigger a re-render for tooltip changes
                StateHasChanged();
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
                    
                    // Get labels if label getters are available
                    var sourceLabel = sourceLabelGetter != null ? sourceLabelGetter(item) : source;
                    var targetLabel = targetLabelGetter != null ? targetLabelGetter(item) : target;
                    
                    // Create or update source node
                    if (!nodeMap.ContainsKey(source))
                    {
                        nodeMap[source] = new SankeyNode 
                        { 
                            Id = source,
                            Label = sourceLabel
                        };
                    }
                    
                    // Create or update target node
                    if (!nodeMap.ContainsKey(target))
                    {
                        nodeMap[target] = new SankeyNode 
                        { 
                            Id = target,
                            Label = targetLabel
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

        /// <summary>
        /// Shows tooltip for a node.
        /// </summary>
        private void ShowNodeTooltip(MouseEventArgs args, ComputedSankeyNode node)
        {
            if (TooltipService == null) return;
            
            // Store current tooltip node to prevent re-opening
            if (currentTooltipNode == node) return;
            currentTooltipNode = node;
            
            var tooltip = new RenderFragment(builder =>
            {
                builder.OpenComponent<Rendering.ChartTooltip>(0);
                builder.AddAttribute(1, "Title", node.Label ?? node.Id);
                
                // Build custom content for node tooltip
                var content = new RenderFragment(contentBuilder =>
                {
                    // Add the title
                    contentBuilder.OpenElement(0, "div");
                    contentBuilder.AddAttribute(1, "class", "rz-chart-tooltip-title");
                    contentBuilder.AddContent(2, node.Label ?? node.Id);
                    contentBuilder.CloseElement();
                    
                    var valueStr = ValueFormatter != null ? ValueFormatter(node.ComputedValue) : $"{node.ComputedValue:N0}";
                    
                    contentBuilder.OpenElement(3, "div");
                    contentBuilder.AddContent(4, $"{ValueText}: ");
                    contentBuilder.OpenElement(5, "span");
                    contentBuilder.AddAttribute(6, "class", "rz-chart-tooltip-item-value");
                    contentBuilder.AddContent(7, valueStr);
                    contentBuilder.CloseElement();
                    contentBuilder.CloseElement();
                    
                    if (node.SourceLinks.Any())
                    {
                        var outgoingValue = node.SourceLinks.Sum(l => l.Value);
                        var outgoingStr = ValueFormatter != null ? ValueFormatter(outgoingValue) : $"{outgoingValue:N0}";
                        
                        contentBuilder.OpenElement(8, "div");
                        contentBuilder.AddContent(9, $"{OutgoingText}: ");
                        contentBuilder.OpenElement(10, "span");
                        contentBuilder.AddAttribute(11, "class", "rz-chart-tooltip-item-value");
                        contentBuilder.AddContent(12, outgoingStr);
                        contentBuilder.CloseElement();
                        contentBuilder.CloseElement();
                    }
                    
                    if (node.TargetLinks.Any())
                    {
                        var incomingValue = node.TargetLinks.Sum(l => l.Value);
                        var incomingStr = ValueFormatter != null ? ValueFormatter(incomingValue) : $"{incomingValue:N0}";
                        
                        contentBuilder.OpenElement(13, "div");
                        contentBuilder.AddContent(14, $"{IncomingText}: ");
                        contentBuilder.OpenElement(15, "span");
                        contentBuilder.AddAttribute(16, "class", "rz-chart-tooltip-item-value");
                        contentBuilder.AddContent(17, incomingStr);
                        contentBuilder.CloseElement();
                        contentBuilder.CloseElement();
                    }
                });
                
                builder.AddAttribute(2, "ChildContent", content);
                if (!string.IsNullOrEmpty(TooltipStyle))
                {
                    builder.AddAttribute(3, "Style", TooltipStyle);
                }
                builder.CloseComponent();
            });
            
            // Use chart tooltip with offset to prevent flickering
            TooltipService.OpenChartTooltip(Element, args.OffsetX + 15, args.OffsetY - 5, _ => tooltip, new ChartTooltipOptions());
        }

        /// <summary>
        /// Shows tooltip for a link.
        /// </summary>
        private void ShowLinkTooltip(MouseEventArgs args, ComputedSankeyLink link)
        {
            if (TooltipService == null) return;
            
            // Store current tooltip link to prevent re-opening
            if (currentTooltipLink == link) return;
            currentTooltipLink = link;
            
            var sourceLabel = link.SourceNode?.Label ?? link.Source;
            var targetLabel = link.TargetNode?.Label ?? link.Target;
            var valueStr = ValueFormatter != null ? ValueFormatter(link.Value) : $"{link.Value:N0}";
            
            var tooltip = new RenderFragment(builder =>
            {
                builder.OpenComponent<Rendering.ChartTooltip>(0);
                builder.AddAttribute(1, "Title", $"{sourceLabel} → {targetLabel}");
                builder.AddAttribute(2, "Label", FlowText);
                builder.AddAttribute(3, "Value", valueStr);
                if (!string.IsNullOrEmpty(TooltipStyle))
                {
                    builder.AddAttribute(4, "Style", TooltipStyle);
                }
                builder.CloseComponent();
            });
            
            // Use chart tooltip with offset to prevent flickering
            TooltipService.OpenChartTooltip(Element, args.OffsetX + 15, args.OffsetY - 5, _ => tooltip, new ChartTooltipOptions());
        }
        
        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        private void HideTooltip()
        {
            currentTooltipNode = null;
            currentTooltipLink = null;
            TooltipService?.Close();
        }
        
        // Track current tooltip to prevent flickering
        private ComputedSankeyNode currentTooltipNode;
        private ComputedSankeyLink currentTooltipLink;

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