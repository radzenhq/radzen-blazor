using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCarousel component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenCarousel Change=@(args => Console.WriteLine($"Selected index is: {args}"))&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenCarouselItem&gt;
    ///             Details for Orders
    ///         &lt;/RadzenCarouselItem&gt;
    ///         &lt;RadzenCarousel&gt;
    ///             Details for Employees
    ///         &lt;/RadzenCarouselItem&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenCarousel&gt;
    /// </code>
    /// </example>
    public partial class RadzenCarousel : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        [Parameter]
        public RenderFragment Items { get; set; }

        internal List<RadzenCarouselItem> items = new List<RadzenCarouselItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenCarouselItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);

                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveItem(RadzenCarouselItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);

                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-carousel {(AllowNavigation ? "" : "rz-carousel-no-navigation")} {(PagerOverlay ? "rz-carousel-pager-overlay" : "")}".Trim();
        }

        /// <summary>
        /// Navigates to specific index.
        /// </summary>
        public async Task Navigate(int index)
        {
            if (Auto)
            {
                await Reset();
            }

            await GoTo(index);
        }

        async Task Prev()
        {
            await Navigate(selectedIndex == 0 ? items.Count - 1 : selectedIndex - 1);
        }

        async Task Next()
        {
            await Navigate(selectedIndex == items.Count - 1 ? 0 : selectedIndex + 1);
        }

        async Task GoTo(int index)
        {
            if (index >= 0 && index <= items.Count - 1 && selectedIndex != index)
            {
                selectedIndex = index;
                await SelectedIndexChanged.InvokeAsync(selectedIndex);
                await Change.InvokeAsync(selectedIndex);
                await JSRuntime.InvokeVoidAsync("Radzen.scrollCarouselItem", items[selectedIndex].element);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Stops the auto-cycle timer.
        /// </summary>
        public void Stop()
        {
            timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Starts the auto-cycle timer.
        /// </summary>
        public void Start()
        {
            timer?.Change(TimeSpan.FromMilliseconds(Interval), TimeSpan.FromMilliseconds(Interval));
        }

        /// <summary>
        /// Resets the auto-cycle timer.
        /// </summary>
        public async Task Reset()
        {
            Stop();
            Start();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets or sets the selected index.
        /// </summary>
        /// <value>The selected index.</value>
        [Parameter]
        public int SelectedIndex { get; set; }

        private int selectedIndex;

        /// <summary>
        /// Gets or sets the selected index changed callback.
        /// </summary>
        /// <value>The selected index changed callback.</value>
        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        /// <summary>
        /// Gets or sets the change callback.
        /// </summary>
        /// <value>The change callback.</value>
        [Parameter]
        public EventCallback<int> Change { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldUpdate = false;
            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                selectedIndex = parameters.GetValueOrDefault<int>(nameof(SelectedIndex));
                shouldUpdate = true;
            }

            if (parameters.DidParameterChange(nameof(Auto), Auto) ||
                    parameters.DidParameterChange(nameof(Interval), Interval))
            {
                if (parameters.GetValueOrDefault<bool>(nameof(Auto)))
                {
                    await Reset();
                }
                else
                {
                    Stop();
                }
            }

            await base.SetParametersAsync(parameters);

            if (shouldUpdate)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.scrollCarouselItem", items[selectedIndex].element);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenCarousel"/> cycle is automatic.
        /// </summary>
        /// <value><c>true</c> if cycle automatic; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Auto { get; set; } = true;

        /// <summary>
        /// Gets or sets the auto-cycle interval in milliseconds.
        /// </summary>
        [Parameter]
        public double Interval { get; set; } = 4000;

        /// <summary>
        /// Gets or sets the pager position. Set to <c>PagerPosition.Bottom</c> by default.
        /// </summary>
        /// <value>The pager position.</value>
        [Parameter]
        public PagerPosition PagerPosition { get; set; } = PagerPosition.Bottom;

        /// <summary>
        /// Gets or sets a value indicating whether pager overlays the carousel items. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if pager overlay is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PagerOverlay { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether paging is allowed. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if paging is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowPaging { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether previous/next navigation is allowed. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if previous/next navigation is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowNavigation { get; set; } = true;

        /// <summary>
        /// Gets or sets the buttons style
        /// </summary>
        /// <value>The buttons style.</value>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Base;

        /// <summary>
        /// Gets or sets the design variant of the buttons.
        /// </summary>
        /// <value>The variant of the buttons.</value>
        [Parameter]
        public Variant ButtonVariant { get; set; } = Variant.Text;

        /// <summary>
        /// Gets or sets the color shade of the buttons.
        /// </summary>
        /// <value>The color shade of the buttons.</value>
        [Parameter]
        public Shade ButtonShade { get; set; } = Shade.Lighter;

        /// <summary>
        /// Gets or sets the buttons size.
        /// </summary>
        /// <value>The buttons size.</value>
        [Parameter]
        public ButtonSize ButtonSize { get; set; } = ButtonSize.Large;

        /// <summary>
        /// Gets or sets the next button text.
        /// </summary>
        /// <value>The next button text.</value>
        [Parameter]
        public string NextText { get; set; } = "";

        /// <summary>
        /// Gets or sets the previous button text.
        /// </summary>
        /// <value>The previous button text.</value>
        [Parameter]
        public string PrevText { get; set; } = "";

        /// <summary>
        /// Gets or sets the next button icon.
        /// </summary>
        /// <value>The next button icon.</value>
        [Parameter]
        public string NextIcon { get; set; } = "arrow_forward_ios";

        /// <summary>
        /// Gets or sets the previous button icon.
        /// </summary>
        /// <value>The previous button icon.</value>
        [Parameter]
        public string PrevIcon { get; set; } = "arrow_back_ios_new";

        System.Threading.Timer timer;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                var ts = TimeSpan.FromMilliseconds(Interval);
                timer = new System.Threading.Timer(state => InvokeAsync(Next), 
                    null, Auto ? ts : Timeout.InfiniteTimeSpan, ts);
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        double? x;
        double? y;

        void OnTouchStart(TouchEventArgs args)
        {
            x = args.Touches[0].ClientX;
            y = args.Touches[0].ClientY;
        }

        async Task OnTouchEnd(TouchEventArgs args)
        {
            if (x == null || y == null)
            {
                return;
            }

            var xDiff = x.Value - args.ChangedTouches[0].ClientX;
            var yDiff = y.Value - args.ChangedTouches[0].ClientY;

            if (Math.Abs(xDiff) < 100 && Math.Abs(yDiff) < 100)
            {
                x = null;
                y = null;
                return;
            }

            if (Math.Abs(xDiff) > Math.Abs(yDiff))
            {
                if (xDiff > 0)
                {
                    await Next();
                }
                else
                {
                    await Prev();
                }
            }

            x = null;
            y = null;
        }

        void OnTouchCancel(TouchEventArgs args)
        {
            x = null;
            y = null;
        }
    }
}
