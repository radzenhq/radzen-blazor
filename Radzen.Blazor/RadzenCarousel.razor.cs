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
            return $"rz-carousel {(AllowNavigation ? "" : "rz-carousel-no-navigation")}".Trim();
        }

        /// <summary>
        /// Navigates to specific index.
        /// </summary>
        public async Task Navigate(int index)
        {
            if (selectedIndex != index)
            {
                selectedIndex = index == items.Count ? 0 : index;
                await SelectedIndexChanged.InvokeAsync(selectedIndex);
                await Change.InvokeAsync(selectedIndex);
                await JSRuntime.InvokeVoidAsync("Radzen.scrollCarouselItem", items[selectedIndex].element);
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
            timer?.Change(TimeSpan.FromMilliseconds(Interval), TimeSpan.Zero);
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

        System.Threading.Timer timer;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                var ts = TimeSpan.FromMilliseconds(Interval);
                timer = new System.Threading.Timer(state => InvokeAsync(() => Navigate(selectedIndex + 1)), 
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
    }
}
