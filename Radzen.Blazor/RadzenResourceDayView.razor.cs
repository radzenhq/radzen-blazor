using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Globalization;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the appointments of multiple resources (rooms, people, equipment) side by side for a single day in <see cref="RadzenScheduler{TItem}" />.
    /// Each resource is rendered as a column. Appointments are matched to a resource by comparing the value of <see cref="ResourceProperty" /> of the
    /// appointment with the value of <see cref="ValueProperty" /> of the resource.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenScheduler Data="@appointments" StartProperty="Start" EndProperty="End" TextProperty="Text"&gt;
    ///     &lt;RadzenResourceDayView Data="@rooms" TextProperty="Name" ValueProperty="Id" ResourceProperty="RoomId" /&gt;
    /// &lt;/RadzenScheduler&gt;
    /// </code>
    /// </example>
    public partial class RadzenResourceDayView : SchedulerViewBase
    {
        /// <inheritdoc />
        public override string Icon => "view_column";

        private string? text;

        /// <inheritdoc />
        [Parameter]
        public override string Text
        {
            get => text ?? Localize(nameof(RadzenStrings.Scheduler_ResourcesText));
            set => text = value;
        }

        /// <summary>
        /// Gets or sets the collection of resources displayed as columns.
        /// </summary>
        /// <value>The resources.</value>
        [Parameter]
        public IEnumerable? Data { get; set; }

        /// <summary>
        /// Specifies the property of a resource item which contains its display text. The resource item itself is used when not set.
        /// </summary>
        /// <value>The name of the property.</value>
        [Parameter]
        public string? TextProperty { get; set; }

        /// <summary>
        /// Specifies the property of a resource item which contains its unique value. The resource item itself is used when not set.
        /// </summary>
        /// <value>The name of the property.</value>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Specifies the property of an appointment data item which contains the value of the resource it belongs to.
        /// The property value is compared to the value of <see cref="ValueProperty" /> of every resource. Collection properties are
        /// supported - the appointment is displayed for every resource whose value the collection contains.
        /// </summary>
        /// <value>The name of the property.</value>
        [Parameter]
        public string? ResourceProperty { get; set; }

        /// <summary>
        /// Gets or sets the template used to render the resource column headers. The resource item is passed as context.
        /// </summary>
        /// <value>The header template.</value>
        [Parameter]
        public RenderFragment<object>? HeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a dedicated all-day row is displayed. Appointments which span the entire visible day are
        /// displayed in it instead of the time grid. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if the all-day row is visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowAllDay { get; set; } = true;

        private string? allDayText;

        /// <summary>
        /// Gets or sets the text displayed in the header of the all-day row. Set to <c>All day</c> by default.
        /// </summary>
        /// <value>The all-day text.</value>
        [Parameter]
        public string AllDayText
        {
            get => allDayText ?? Localize(nameof(RadzenStrings.Scheduler_AllDayText));
            set => allDayText = value;
        }

        /// <summary>
        /// Gets or sets the time format.
        /// </summary>
        /// <value>The time format. Set to <c>h tt</c> by default.</value>
        [Parameter]
        public string TimeFormat { get; set; } = "h tt";

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        /// <value>The start time.</value>
        [Parameter]
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(8);

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        /// <value>The end time.</value>
        [Parameter]
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets slot size in minutes. Set to <c>30</c> by default.
        /// </summary>
        /// <value>The slot size in minutes.</value>
        [Parameter]
        public int MinutesPerSlot { get; set; } = 30;

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                var culture = Scheduler?.Culture ?? CultureInfo.CurrentCulture;
                return Scheduler?.CurrentDate.ToString(Scheduler.Culture.DateTimeFormat.ShortDatePattern ?? "d", culture) ?? "";
            }
        }

        /// <inheritdoc />
        public override DateTime StartDate
        {
            get
            {
                return Scheduler?.CurrentDate.Date.Add(StartTime) ?? DateTime.Today.Add(StartTime);
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                return Scheduler?.CurrentDate.Date.Add(EndTime) ?? DateTime.Today.Add(EndTime);
            }
        }

        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler?.CurrentDate.Date.AddDays(1) ?? DateTime.Today.AddDays(1);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler?.CurrentDate.Date.AddDays(-1) ?? DateTime.Today.AddDays(-1);
        }
    }
}
