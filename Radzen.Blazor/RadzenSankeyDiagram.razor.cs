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
    /// <typeparam name="TNode">The type of node data item.</typeparam>
    /// <typeparam name="TLink">The type of link data item.</typeparam>
    public partial class RadzenSankeyDiagram<TNode, TLink> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the node data.
        /// </summary>
        [Parameter]
        public IEnumerable<TNode> Nodes { get; set; }

        /// <summary>
        /// Gets or sets the link data.
        /// </summary>
        [Parameter]
        public IEnumerable<TLink> Links { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TNode" /> which provides the node ID.
        /// </summary>
        [Parameter]
        public string NodeIdProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TNode" /> which provides the node label.
        /// </summary>
        [Parameter]
        public string NodeLabelProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TNode" /> which provides the node fill color.
        /// </summary>
        [Parameter]
        public string NodeFillProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TNode" /> which provides the node stroke color.
        /// </summary>
        [Parameter]
        public string NodeStrokeProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TNode" /> which provides the node value (optional).
        /// </summary>
        [Parameter]
        public string NodeValueProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TLink" /> which provides the source node ID.
        /// </summary>
        [Parameter]
        public string LinkSourceProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TLink" /> which provides the target node ID.
        /// </summary>
        [Parameter]
        public string LinkTargetProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TLink" /> which provides the link value.
        /// </summary>
        [Parameter]
        public string LinkValueProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TLink" /> which provides the link fill color.
        /// </summary>
        [Parameter]
        public string LinkFillProperty { get; set; }


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
        [Parameter]
        public double MarginLeft { get; set; } = 80;

        /// <summary>
        /// Gets or sets the top margin.
        /// </summary>
        [Parameter]
        public double MarginTop { get; set; } = 10;

        /// <summary>
        /// Gets or sets the right margin.
        /// </summary>
        [Parameter]
        public double MarginRight { get; set; } = 80;

        /// <summary>
        /// Gets or sets the bottom margin.
        /// </summary>
        [Parameter]
        public double MarginBottom { get; set; } = 10;

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
        /// Gets or sets the node sort function.
        /// </summary>
        [Parameter]
        public Func<SankeyNode, SankeyNode, int> NodeSort { get; set; }

        /// <summary>
        /// Gets or sets the link sort function.
        /// </summary>
        [Parameter]
        public Func<SankeyLink, SankeyLink, int> LinkSort { get; set; }

        /// <summary>
        /// Gets the computed nodes with layout.
        /// </summary>
        public IList<ComputedSankeyNode> ComputedNodes { get; private set; }

        /// <summary>
        /// Gets the computed links with paths.
        /// </summary>
        public IList<ComputedSankeyLink> ComputedLinks { get; private set; }

        // Property accessors
        private Func<TNode, string> nodeIdGetter;
        private Func<TNode, string> nodeLabelGetter;
        private Func<TNode, string> nodeFillGetter;
        private Func<TNode, string> nodeStrokeGetter;
        private Func<TNode, double?> nodeValueGetter;
        private Func<TLink, string> linkSourceGetter;
        private Func<TLink, string> linkTargetGetter;
        private Func<TLink, double> linkValueGetter;
        private Func<TLink, string> linkFillGetter;




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
            
            // If using the built-in SankeyNode type and no property mappings specified, set defaults
            if (typeof(TNode) == typeof(SankeyNode))
            {
                NodeIdProperty = NodeIdProperty ?? nameof(SankeyNode.Id);
                NodeLabelProperty = NodeLabelProperty ?? nameof(SankeyNode.Label);
                NodeFillProperty = NodeFillProperty ?? nameof(SankeyNode.Fill);
                NodeStrokeProperty = NodeStrokeProperty ?? nameof(SankeyNode.Stroke);
                NodeValueProperty = NodeValueProperty ?? nameof(SankeyNode.Value);
                
                // Initialize getters for built-in types
                nodeIdGetter = PropertyAccess.Getter<TNode, string>(NodeIdProperty);
                nodeLabelGetter = PropertyAccess.Getter<TNode, string>(NodeLabelProperty);
                nodeFillGetter = string.IsNullOrEmpty(NodeFillProperty) ? null : PropertyAccess.Getter<TNode, string>(NodeFillProperty);
                nodeStrokeGetter = string.IsNullOrEmpty(NodeStrokeProperty) ? null : PropertyAccess.Getter<TNode, string>(NodeStrokeProperty);
                nodeValueGetter = string.IsNullOrEmpty(NodeValueProperty) ? null : PropertyAccess.Getter<TNode, double?>(NodeValueProperty);
            }
            
            // If using the built-in SankeyLink type and no property mappings specified, set defaults
            if (typeof(TLink) == typeof(SankeyLink))
            {
                LinkSourceProperty = LinkSourceProperty ?? nameof(SankeyLink.Source);
                LinkTargetProperty = LinkTargetProperty ?? nameof(SankeyLink.Target);
                LinkValueProperty = LinkValueProperty ?? nameof(SankeyLink.Value);
                LinkFillProperty = LinkFillProperty ?? nameof(SankeyLink.Fill);
                
                // Initialize getters for built-in types
                linkSourceGetter = PropertyAccess.Getter<TLink, string>(LinkSourceProperty);
                linkTargetGetter = PropertyAccess.Getter<TLink, string>(LinkTargetProperty);
                linkValueGetter = PropertyAccess.Getter<TLink, double>(LinkValueProperty);
                linkFillGetter = string.IsNullOrEmpty(LinkFillProperty) ? null : PropertyAccess.Getter<TLink, string>(LinkFillProperty);
            }
            
            Initialize();
            ComputeLayout();
        }

        private void Initialize()
        {
            // Extract width and height from CurrentStyle if available
            if (CurrentStyle.ContainsKey("height"))
            {
                var pixelHeight = CurrentStyle["height"];
                if (pixelHeight.EndsWith("px"))
                {
                    Height = Convert.ToDouble(pixelHeight.TrimEnd("px".ToCharArray()), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            if (CurrentStyle.ContainsKey("width"))
            {
                var pixelWidth = CurrentStyle["width"];
                if (pixelWidth.EndsWith("px"))
                {
                    Width = Convert.ToDouble(pixelWidth.TrimEnd("px".ToCharArray()), System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool shouldUpdate = false;

            if (parameters.DidParameterChange(nameof(NodeIdProperty), NodeIdProperty) || (nodeIdGetter == null && !string.IsNullOrEmpty(NodeIdProperty)))
            {
                var property = parameters.GetValueOrDefault<string>(nameof(NodeIdProperty)) ?? NodeIdProperty;
                if (!string.IsNullOrEmpty(property))
                {
                    nodeIdGetter = PropertyAccess.Getter<TNode, string>(property);
                    shouldUpdate = true;
                }
            }

            if (parameters.DidParameterChange(nameof(NodeLabelProperty), NodeLabelProperty))
            {
                nodeLabelGetter = PropertyAccess.Getter<TNode, string>(parameters.GetValueOrDefault<string>(nameof(NodeLabelProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(NodeFillProperty), NodeFillProperty))
            {
                nodeFillGetter = string.IsNullOrEmpty(parameters.GetValueOrDefault<string>(nameof(NodeFillProperty))) 
                    ? null 
                    : PropertyAccess.Getter<TNode, string>(parameters.GetValueOrDefault<string>(nameof(NodeFillProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(NodeStrokeProperty), NodeStrokeProperty))
            {
                nodeStrokeGetter = string.IsNullOrEmpty(parameters.GetValueOrDefault<string>(nameof(NodeStrokeProperty))) 
                    ? null 
                    : PropertyAccess.Getter<TNode, string>(parameters.GetValueOrDefault<string>(nameof(NodeStrokeProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(NodeValueProperty), NodeValueProperty))
            {
                nodeValueGetter = string.IsNullOrEmpty(parameters.GetValueOrDefault<string>(nameof(NodeValueProperty))) 
                    ? null 
                    : PropertyAccess.Getter<TNode, double?>(parameters.GetValueOrDefault<string>(nameof(NodeValueProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(LinkSourceProperty), LinkSourceProperty))
            {
                linkSourceGetter = PropertyAccess.Getter<TLink, string>(parameters.GetValueOrDefault<string>(nameof(LinkSourceProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(LinkTargetProperty), LinkTargetProperty))
            {
                linkTargetGetter = PropertyAccess.Getter<TLink, string>(parameters.GetValueOrDefault<string>(nameof(LinkTargetProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(LinkValueProperty), LinkValueProperty))
            {
                linkValueGetter = PropertyAccess.Getter<TLink, double>(parameters.GetValueOrDefault<string>(nameof(LinkValueProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(LinkFillProperty), LinkFillProperty))
            {
                linkFillGetter = string.IsNullOrEmpty(parameters.GetValueOrDefault<string>(nameof(LinkFillProperty))) 
                    ? null 
                    : PropertyAccess.Getter<TLink, string>(parameters.GetValueOrDefault<string>(nameof(LinkFillProperty)));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(Nodes), Nodes) || 
                parameters.DidParameterChange(nameof(Links), Links) ||
                parameters.DidParameterChange(nameof(NodeWidth), NodeWidth) ||
                parameters.DidParameterChange(nameof(NodePadding), NodePadding) ||
                parameters.DidParameterChange(nameof(NodeAlignment), NodeAlignment) ||
                parameters.DidParameterChange(nameof(ColorScheme), ColorScheme) ||
                parameters.DidParameterChange(nameof(MarginLeft), MarginLeft) ||
                parameters.DidParameterChange(nameof(MarginRight), MarginRight) ||
                parameters.DidParameterChange(nameof(MarginTop), MarginTop) ||
                parameters.DidParameterChange(nameof(MarginBottom), MarginBottom) ||
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
        public void Resize(double width, double height)
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
                InvokeAsync(StateHasChanged);
            }
        }

        private void ComputeLayout()
        {
            if (Nodes == null || Links == null || !Width.HasValue || !Height.HasValue || Width <= 0 || Height <= 0)
            {
                ComputedNodes = null;
                ComputedLinks = null;
                return;
            }

            if (nodeIdGetter == null || linkSourceGetter == null || linkTargetGetter == null || linkValueGetter == null)
            {
                ComputedNodes = null;
                ComputedLinks = null;
                return;
            }

            try
            {

                // Convert generic data to SankeyNode/SankeyLink
                var sankeyNodes = new List<SankeyNode>();
                var nodesList = Nodes.ToList();
                foreach (var node in nodesList)
                {
                    sankeyNodes.Add(new SankeyNode
                    {
                        Id = nodeIdGetter(node),
                        Label = nodeLabelGetter?.Invoke(node),
                        Fill = nodeFillGetter?.Invoke(node),
                        Stroke = nodeStrokeGetter?.Invoke(node),
                        Value = nodeValueGetter?.Invoke(node)
                    });
                }

                var sankeyLinks = new List<SankeyLink>();
                var linksList = Links.ToList();
                foreach (var link in linksList)
                {
                    sankeyLinks.Add(new SankeyLink
                    {
                        Source = linkSourceGetter(link),
                        Target = linkTargetGetter(link),
                        Value = linkValueGetter(link),
                        Fill = linkFillGetter?.Invoke(link)
                    });
                }

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
                
                // Assign color indexes to nodes without explicit fill colors
                if (ComputedNodes != null)
                {
                    var colorIndex = 0;
                    foreach (var node in ComputedNodes)
                    {
                        if (string.IsNullOrEmpty(node.Fill))
                        {
                            node.ColorIndex = colorIndex++;
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