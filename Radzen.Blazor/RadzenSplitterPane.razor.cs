using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSplitterPane component.
    /// </summary>
    public partial class RadzenSplitterPane : RadzenComponent
    {
        private RadzenSplitter? splitter;
        private string? size;

        internal string? SizeRuntine { get; set; }

        internal bool SizeAuto { get; set; }

        internal int Index { get; set; }

        internal bool IsLastResizable
        {
            get { return Splitter?.Panes.LastOrDefault(o => o.Resizable && !o.GetCollapsed()) == this; }
        }

        internal bool IsLast => Splitter?.Panes.Count - 1 == Index;

        internal RadzenSplitterPane? Next()
        {
            if (Splitter == null)
            {
                return null;
            }

            return Index <= Splitter.Panes.Count - 2
                ? Splitter.Panes[Index + 1]
                : null;
        }

        internal bool IsResizable
        {
            get
            {
                var paneNext = Next();

                if (GetCollapsed()
                    || (Splitter != null && Index == Splitter.Panes.Count - 2 && paneNext?.IsResizable == false)
                    || (IsLastResizable && paneNext != null && paneNext.GetCollapsed())
                    )
                {
                    return false;
                }

                return Resizable;
            }
        }

        internal bool IsCollapsible
        {
            get
            {
                if (Collapsible && !GetCollapsed())
                {
                    return true;
                }

                var paneNext = Next();
                if (paneNext == null)
                {
                    return false;
                }

                return paneNext.IsLast && paneNext.Collapsible && paneNext.GetCollapsed();
            }
        }

        internal bool IsExpandable
        {
            get
            {
                if (GetCollapsed())
                {
                    return true;
                }

                var paneNext = Next();
                if (paneNext == null)
                {
                    return false;
                }

                return paneNext.IsLast && paneNext.Collapsible && !paneNext.GetCollapsed();
            }
        }

        internal string AriaExpanded => (!GetCollapsed()).ToString(CultureInfo.InvariantCulture).ToLowerInvariant();

        internal string ClassName
        {
            get
            {
                if (GetCollapsed())
                {
                    return "collapsed";
                }

                if (IsLastResizable)
                {
                    return "lastresizable";
                }

                if (IsResizable)
                {
                    return "resizable";
                }

                return "locked";
            }
        }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitterPane"/> is collapsible.
        /// </summary>
        /// <value><c>true</c> if collapsible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Collapsible { get; set; } = true;

        private bool? collapsed;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitterPane"/> is collapsed.
        /// </summary>
        /// <value><c>true</c> if collapsed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Collapsed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitterPane"/> is resizable.
        /// </summary>
        /// <value><c>true</c> if resizable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Resizable { get; set; } = true;

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        [Parameter]
        public string? Max { get; set; }

        /// <summary>
        /// Determines the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        [Parameter]
        public string? Min { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public string? Size
        {
            get => SizeRuntine ?? size;
            set => size = value;
        }

        /// <summary>
        /// Gets or sets the visibility of the splitter bar.
        /// </summary>
        /// <value>The visibility of the splitter bar.</value>
        [Parameter]
        public bool BarVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets the splitter.
        /// </summary>
        /// <value>The splitter.</value>
        [CascadingParameter]
        public RadzenSplitter? Splitter
        {
            get => splitter;
            set
            {
                if (splitter != value)
                {
                    splitter = value;
                    splitter?.AddPane(this);
                }
            }
        }

        internal Orientation BarOrientation => Splitter?.Orientation == Orientation.Vertical ? Orientation.Vertical : Orientation.Horizontal;

        internal string AriaOrientation => Splitter?.Orientation == Orientation.Vertical ? "horizontal" : "vertical";

        internal string? AriaControls => GetId();

        /// <summary>
        /// Gets or sets the id of an element that labels the resize separator. When set, it is exposed as <c>aria-labelledby</c> and takes precedence over the generated <see cref="ResizeAriaLabel"/>.
        /// </summary>
        /// <value>The id of the labelling element.</value>
        [Parameter]
        public string? AriaLabelledBy { get; set; }

        private string? resizeAriaLabel;

        /// <summary>
        /// Gets or sets the accessible label of the resize separator. Defaults to a localizable "Resize {0}" text.
        /// </summary>
        /// <value>The accessible label of the resize separator.</value>
        [Parameter]
        public string? ResizeAriaLabel
        {
            get => resizeAriaLabel ?? string.Format(CultureInfo.CurrentCulture, Localize(nameof(RadzenStrings.Splitter_ResizeAriaLabel)), GetId());
            set => resizeAriaLabel = value;
        }

        private string? collapseAriaLabel;

        /// <summary>
        /// Gets or sets the accessible label of the collapse button. Defaults to a localizable "Collapse pane" text.
        /// </summary>
        /// <value>The accessible label of the collapse button.</value>
        [Parameter]
        public string? CollapseAriaLabel
        {
            get => collapseAriaLabel ?? Localize(nameof(RadzenStrings.Splitter_CollapseAriaLabel));
            set => collapseAriaLabel = value;
        }

        private string? expandAriaLabel;

        /// <summary>
        /// Gets or sets the accessible label of the expand button. Defaults to a localizable "Expand pane" text.
        /// </summary>
        /// <value>The accessible label of the expand button.</value>
        [Parameter]
        public string? ExpandAriaLabel
        {
            get => expandAriaLabel ?? Localize(nameof(RadzenStrings.Splitter_ExpandAriaLabel));
            set => expandAriaLabel = value;
        }

        internal string? AriaLabel => AriaLabelledBy != null ? null : ResizeAriaLabel;

        static double? ParsePercent(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();

            if (value.EndsWith('%')
                && double.TryParse(value.AsSpan(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        internal double AriaValueMin => ParsePercent(Min) ?? 0d;

        internal double AriaValueMax => ParsePercent(Max) ?? 100d;

        internal double AriaValueNow
        {
            get
            {
                var value = ParsePercent(Size) ?? AriaValueMin;

                if (value < AriaValueMin)
                {
                    value = AriaValueMin;
                }

                if (value > AriaValueMax)
                {
                    value = AriaValueMax;
                }

                return Math.Round(value);
            }
        }

        internal void SetCollapsed(bool value)
        {
            collapsed = value;
        }

        internal bool GetCollapsed()
        {
            return collapsed ?? Collapsed;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            Splitter?.RemovePane(this);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Collapsed), Collapsed))
            {
                collapsed = parameters.GetValueOrDefault<bool>(nameof(Collapsed));
            }

            if (parameters.DidParameterChange(nameof(Size), Size))
            {
                SizeRuntine = parameters.GetValueOrDefault<string>(nameof(Size));
            }

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            if (Attributes != null && Attributes.TryGetValue("class", out var @class) && !string.IsNullOrEmpty(Convert.ToString(@class, CultureInfo.InvariantCulture)))
            {
                return $"rz-splitter-pane rz-splitter-pane-{ClassName} {@class}";
            }

            return $"rz-splitter-pane rz-splitter-pane-{ClassName}";
        }

        /// <summary>
        /// Gets the component bar CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string GetComponentBarCssClass()
        {
            return $"rz-splitter-bar rz-splitter-bar-{ClassName}";
        }

        bool preventKeyPress;
        bool stopKeydownPropagation;
        bool stopKeypressPropagation;

        static bool IsArrowKey(string? key)
        {
            return key == "ArrowLeft" || key == "ArrowRight" || key == "ArrowUp" || key == "ArrowDown";
        }

        async Task OnKeyPress(KeyboardEventArgs args, bool? expand = null)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;
                stopKeypressPropagation = true;

                if (expand == null)
                {
                    stopKeydownPropagation = true;

                    if (Splitter != null)
                    {
                        if (GetCollapsed())
                        {
                            await Splitter.OnExpand(Index);
                        }
                        else if (IsCollapsible)
                        {
                            await Splitter.OnCollapse(Index);
                        }
                    }

                    return;
                }

                string? id = null;

                if (expand == true && Splitter != null)
                {
                    id = GetId() + "-collapse";
                    await Splitter.OnExpand(Index);
                }
                else if (expand == false && Splitter != null)
                {
                    id = GetId() + "-expand";
                    await Splitter.OnCollapse(Index);
                }

                if (!string.IsNullOrEmpty(id) && JSRuntime != null)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.delayedFocus", id, 200);
                }
            }
            else if (IsArrowKey(key) || key == "Home" || key == "End")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                if (JSRuntime == null || Splitter == null)
                {
                    return;
                }

                var rect = await JSRuntime.InvokeAsync<Rect>("Radzen.clientRect", GetId() + "-resize");
                var splitterRect = await JSRuntime.InvokeAsync<Rect>("Radzen.clientRect", Splitter.ElementId);

                await Splitter.StartResize(new PointerEventArgs()
                {
                    ClientX = rect.Left,
                    ClientY = rect.Top
                }, Index);

                var deltaX = 0d;
                var deltaY = 0d;

                var stepX = Math.Max(1d, splitterRect.Width / 100d);
                var stepY = Math.Max(1d, splitterRect.Height / 100d);

                if (key == "Home")
                {
                    if (BarOrientation == Orientation.Horizontal)
                    {
                        deltaX = -100000d;
                    }
                    else
                    {
                        deltaY = -100000d;
                    }
                }
                else if (key == "End")
                {
                    if (BarOrientation == Orientation.Horizontal)
                    {
                        deltaX = 100000d;
                    }
                    else
                    {
                        deltaY = 100000d;
                    }
                }
                else if (BarOrientation == Orientation.Horizontal)
                {
                    deltaX = key == "ArrowLeft" ? -stepX : key == "ArrowRight" ? stepX : 0;
                }
                else
                {
                    deltaY = key == "ArrowUp" ? -stepY : key == "ArrowDown" ? stepY : 0;
                }

                await JSRuntime.InvokeVoidAsync("Radzen.resizeSplitter", UniqueID, new MouseEventArgs()
                {
                    ClientX = rect.Left + deltaX,
                    ClientY = rect.Top + deltaY,
                    Buttons = 1
                });
            }
            else
            {
                preventKeyPress = false;
                stopKeydownPropagation = false;
                stopKeypressPropagation = false;
            }
        }

        async Task OnDoubleClick(MouseEventArgs args)
        {
            // Only handle double-click if the pane or its next pane is collapsible
            if (!IsCollapsible && !IsExpandable)
            {
                return;
            }

            if (Splitter == null)
            {
                return;
            }
            // If the current pane is collapsed, expand it
            if (GetCollapsed())
            {
                await Splitter.OnExpand(Index);
            }
            // If the current pane can be collapsed, collapse it
            else if (IsCollapsible)
            {
                await Splitter.OnCollapse(Index);
            }
            // If the next pane is the last and is expandable, expand it
            else if (IsExpandable)
            {
                await Splitter.OnExpand(Index);
            }
        }
    }
}
