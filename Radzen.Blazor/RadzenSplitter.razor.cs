using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A splitter component that divides space between resizable panes with draggable dividers.
    /// RadzenSplitter creates layouts with user-adjustable panel sizes, ideal for multi-column interfaces or resizable sidebars.
    /// Allows users to customize their workspace by dragging dividers to resize panes.
    /// Common use cases include code editors with resizable file explorer/code/output panes, email clients with adjustable folder list/message list/message preview,
    /// admin dashboards with resizable navigation and content areas, and data analysis tools with adjustable grid/chart/filter panels.
    /// Features resizable panes (drag dividers between panes to adjust sizes), Horizontal (side-by-side) or Vertical (top-to-bottom) orientation,
    /// size control with fixed pixel sizes/percentages/auto-sized panes, min/max constraints to prevent panes from becoming too small or large,
    /// optional collapse/expand functionality per pane, and nested splitters to create complex layouts.
    /// Panes are defined using RadzenSplitterPane components. Use Size property for fixed widths/heights.
    /// </summary>
    /// <example>
    /// Basic horizontal splitter:
    /// <code>
    /// &lt;RadzenSplitter Style="height: 400px;"&gt;
    ///     &lt;RadzenSplitterPane Size="30%"&gt;
    ///         Left sidebar content
    ///     &lt;/RadzenSplitterPane&gt;
    ///     &lt;RadzenSplitterPane&gt;
    ///         Main content (auto-sized)
    ///     &lt;/RadzenSplitterPane&gt;
    /// &lt;/RadzenSplitter&gt;
    /// </code>
    /// Vertical splitter with min/max sizes:
    /// <code>
    /// &lt;RadzenSplitter Orientation="Orientation.Vertical" Style="height: 600px;"&gt;
    ///     &lt;RadzenSplitterPane Size="200px" Min="100px" Max="400px"&gt;
    ///         Top pane (resizable 100-400px)
    ///     &lt;/RadzenSplitterPane&gt;
    ///     &lt;RadzenSplitterPane&gt;
    ///         Bottom pane (fills remaining space)
    ///     &lt;/RadzenSplitterPane&gt;
    /// &lt;/RadzenSplitter&gt;
    /// </code>
    /// </example>
    public partial class RadzenSplitter : RadzenComponent
    {
        private int sizeAutoPanes = 0;

        /// <summary>
        /// Gets or sets the panes to display within the splitter.
        /// Each RadzenSplitterPane represents one resizable section of the splitter.
        /// </summary>
        /// <value>The panes render fragment containing RadzenSplitterPane definitions.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the layout direction of the splitter.
        /// Horizontal arranges panes side-by-side (resizable width), Vertical stacks panes top-to-bottom (resizable height).
        /// </summary>
        /// <value>The orientation. Default is <see cref="Orientation.Horizontal"/>.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        internal List<RadzenSplitterPane> Panes = new List<RadzenSplitterPane>();

        /// <summary>
        /// Adds the pane.
        /// </summary>
        /// <param name="pane">The pane.</param>
        public void AddPane(RadzenSplitterPane pane)
        {
            if (Panes.IndexOf(pane) != -1 || !pane.Visible)
                return;

            if (string.IsNullOrWhiteSpace(pane.Size))
            {
                //no size defined
                pane.SizeAuto = true;
                sizeAutoPanes++;
            }

            pane.Index = Panes.Count;
            Panes.Add(pane);

            foreach (var iPane in Panes)
            {
                if (!iPane.SizeAuto)
                    continue;

                iPane.SizeRuntine = (100 / sizeAutoPanes) + "%";
            }
        }

        /// <summary>
        /// Removes the pane.
        /// </summary>
        /// <param name="pane">The pane.</param>
        public void RemovePane(RadzenSplitterPane pane)
        {
            if (Panes.Contains(pane))
            {
                Panes.Remove(pane);
                try
                {
                    InvokeAsync(StateHasChanged);
                }
                catch
                {
                }
            }
        }


        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            try
            {
                InvokeAsync(StateHasChanged);
            }
            catch
            {
            }
        }

        internal Task StartResize(PointerEventArgs args, int paneIndex)
        {
            var pane = Panes[paneIndex];
            if (!pane.Resizable)
                return Task.CompletedTask;

            var paneNextResizable = Panes.Skip(paneIndex + 1).FirstOrDefault(o => o.Resizable && !o.GetCollapsed());


            return JSRuntime.InvokeVoidAsync("Radzen.startSplitterResize",
                UniqueID,
                Reference,
                pane.UniqueID,
                paneNextResizable?.UniqueID,
                Orientation.ToString(), Orientation == Orientation.Horizontal ? args.ClientX : args.ClientY,
                pane.Min,
                pane.Max,
                paneNextResizable?.Min,
                paneNextResizable?.Max).AsTask();
        }

        /// <summary>
        /// Value indicating if the splitter is resizing.
        /// </summary>
        public bool IsResizing { get; private set; }

        /// <summary>
        /// Called on pane resizing.
        /// </summary>
        [JSInvokable("RadzenSplitter.OnPaneResizing")]
        public async Task OnPaneResizing()
        {
            IsResizing = true;

            StateHasChanged();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when pane resized.
        /// </summary>
        /// <param name="paneIndex">Index of the pane.</param>
        /// <param name="sizeNew">The size new.</param>
        /// <param name="paneNextIndex">Index of the pane next.</param>
        /// <param name="sizeNextNew">The size next new.</param>
        [JSInvokable("RadzenSplitter.OnPaneResized")]
        public async Task OnPaneResized(int paneIndex, double sizeNew, int? paneNextIndex, double? sizeNextNew)
        {
            IsResizing = false;

            var pane = Panes[paneIndex];

            if (Resize.HasDelegate)
            {
                var arg = new RadzenSplitterResizeEventArgs { PaneIndex = pane.Index, Pane = pane, NewSize = sizeNew };
                await Resize.InvokeAsync(arg);
                if (arg.Cancel)
                {
                    var oldSize = pane.SizeRuntine;
                    pane.SizeRuntine = "0";
                    await InvokeAsync(StateHasChanged);
                    pane.SizeRuntine = oldSize;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }

            pane.SizeRuntine = sizeNew.ToString("0.##", CultureInfo.InvariantCulture) + "%";

            if (paneNextIndex.HasValue)
            {
                var paneNext = Panes[paneNextIndex.Value];

                if (Expand.HasDelegate)
                {
                    var arg = new RadzenSplitterResizeEventArgs { PaneIndex = paneNext.Index, Pane = paneNext, NewSize = sizeNextNew.Value };
                    await Resize.InvokeAsync(arg);
                    //cancel omitted because it is managed by the parent panel
                }

                paneNext.SizeRuntine = sizeNextNew.Value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
            }

            StateHasChanged();
        }

        internal async Task OnCollapse(int paneIndex)
        {
            var pane = Panes[paneIndex];
            var paneNext = pane.Next();

            if (paneNext != null && paneNext.Collapsible && paneNext.IsLast && paneNext.GetCollapsed())
            {
                if (Expand.HasDelegate)
                {
                    var arg = new RadzenSplitterEventArgs { PaneIndex = paneNext.Index, Pane = paneNext };
                    await Expand.InvokeAsync(arg);
                    if (arg.Cancel)
                        return;
                }

                paneNext.SetCollapsed(false);
            }
            else
            {
                if (Collapse.HasDelegate)
                {
                    var arg = new RadzenSplitterEventArgs { PaneIndex = pane.Index, Pane = pane };
                    await Collapse.InvokeAsync(arg);
                    if (arg.Cancel)
                        return;
                }

                pane.SetCollapsed(true);
            }

            await InvokeAsync(StateHasChanged);
        }

        internal async Task OnExpand(int paneIndex)
        {
            var pane = Panes[paneIndex];
            var paneNext = pane.Next();

            if (paneNext != null && paneNext.Collapsible && paneNext.IsLast && !pane.GetCollapsed())
            {
                if (Collapse.HasDelegate)
                {
                    var arg = new RadzenSplitterEventArgs { PaneIndex = paneNext.Index, Pane = paneNext };
                    await Collapse.InvokeAsync(arg);
                    if (arg.Cancel)
                        return;
                }

                paneNext.SetCollapsed(true);
            }
            else
            {
                if (Expand.HasDelegate)
                {
                    var arg = new RadzenSplitterEventArgs { PaneIndex = pane.Index, Pane = pane };
                    await Expand.InvokeAsync(arg);
                    if (arg.Cancel)
                        return;
                }

                pane.SetCollapsed(false);
            }

            await InvokeAsync(StateHasChanged);
        }


        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-splitter rz-splitter-{Enum.GetName(typeof(Orientation), Orientation).ToLowerInvariant()}";
        }

        /// <summary>
        /// Gets or sets the collapse callback.
        /// </summary>
        /// <value>The collapse callback.</value>
        [Parameter]
        public EventCallback<RadzenSplitterEventArgs> Collapse { get; set; }

        /// <summary>
        /// Gets or sets the expand callback.
        /// </summary>
        /// <value>The expand callback.</value>
        [Parameter]
        public EventCallback<RadzenSplitterEventArgs> Expand { get; set; }

        /// <summary>
        /// Gets or sets the resize callback.
        /// </summary>
        /// <value>The resize callback.</value>
        [Parameter]
        public EventCallback<RadzenSplitterResizeEventArgs> Resize { get; set; }
    }
}
