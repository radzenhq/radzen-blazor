using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Renders the resources of the scheduler as columns for a single day. Used by <see cref="RadzenDayView" /> when
    /// <see cref="SchedulerViewBase.GroupByResource" /> is set to <c>true</c> and the group orientation is horizontal.
    /// Renders a header row per resource type - the columns are every combination of resource items.
    /// </summary>
    public partial class ResourceDayView : DropableViewBase
    {
        /// <summary>
        /// Gets or sets the start of the visible day.
        /// </summary>
        [Parameter]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end of the visible day.
        /// </summary>
        [Parameter]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        [Parameter]
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        [Parameter]
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Gets or sets the time format.
        /// </summary>
        [Parameter]
        public string? TimeFormat { get; set; }

        /// <summary>
        /// Gets or sets the slot size in minutes.
        /// </summary>
        [Parameter]
        public int MinutesPerSlot { get; set; }

        /// <summary>
        /// Gets or sets the appointments to display.
        /// </summary>
        [Parameter]
        public IList<AppointmentData>? Appointments { get; set; }

        /// <summary>
        /// Gets or sets the resource types to group by in nesting order.
        /// </summary>
        [Parameter]
        public IList<RadzenSchedulerResource> Types { get; set; } = new List<RadzenSchedulerResource>();

        /// <summary>
        /// Gets or sets a value indicating whether the all-day row is displayed.
        /// </summary>
        [Parameter]
        public bool ShowAllDay { get; set; }

        /// <summary>
        /// Gets or sets the text displayed in the header of the all-day row.
        /// </summary>
        [Parameter]
        public string? AllDayText { get; set; }

        /// <summary>
        /// Gets or sets the accessible label of the view content.
        /// </summary>
        [Parameter]
        public string? AriaLabel { get; set; }

        /// <summary>
        /// Gets or sets the scheduler.
        /// </summary>
        [CascadingParameter]
        public IScheduler? Scheduler { get; set; }

        ElementReference view;
        DateTime currentSlot;
        int currentResource;
        bool allDayFocused;
        bool preventKeyPress;
        bool stopKeydownPropagation;

        internal IList<RadzenSchedulerResource> Levels => Types.Where(type => type.Items.Count > 0).ToList();

        internal IList<IDictionary<string, object>> Paths
        {
            get
            {
                IEnumerable<IDictionary<string, object>> paths = new List<IDictionary<string, object>> { new Dictionary<string, object>() };

                foreach (var type in Levels)
                {
                    paths = paths.SelectMany(path => type.Items.Select(item =>
                    {
                        IDictionary<string, object> next = new Dictionary<string, object>(path)
                        {
                            [type.Name] = item
                        };
                        return next;
                    }));
                }

                return paths.ToList();
            }
        }

        internal int SpanOfLevel(int level)
        {
            return Levels.Skip(level + 1).Aggregate(1, (span, type) => span * type.Items.Count);
        }

        internal int RepeatsOfLevel(int level)
        {
            return Levels.Take(level).Aggregate(1, (repeats, type) => repeats * type.Items.Count);
        }

        bool MatchesPath(AppointmentData appointment, IDictionary<string, object> path)
        {
            return path.All(entry => Levels.FirstOrDefault(type => type.Name == entry.Key)?.IsAppointmentInResource(appointment, entry.Value) == true);
        }

        bool IsAllDay(AppointmentData appointment)
        {
            return appointment.Start <= StartDate && appointment.End >= EndDate;
        }

        IList<AppointmentData> AppointmentsForPath(IDictionary<string, object> path)
        {
            if (Appointments == null)
            {
                return Array.Empty<AppointmentData>();
            }

            return Appointments.Where(item => MatchesPath(item, path) && !(ShowAllDay && IsAllDay(item))).ToList();
        }

        internal AppointmentData[] AllDayAppointments(IDictionary<string, object> path)
        {
            if (Appointments == null)
            {
                return Array.Empty<AppointmentData>();
            }

            return Appointments.Where(item => MatchesPath(item, path) && IsAllDay(item)).OrderBy(item => item.Start).ThenByDescending(item => item.End).ToArray();
        }

        AppointmentData[] AppointmentsInSlot(IDictionary<string, object> path, DateTime start, DateTime end)
        {
            if (Appointments == null || Scheduler == null)
            {
                return Array.Empty<AppointmentData>();
            }

            return Appointments.Where(item => MatchesPath(item, path) && Scheduler.IsAppointmentInRange(item, start, end)).OrderBy(item => item.Start).ThenByDescending(item => item.End).ToArray();
        }

        async Task OnSlotClick(DateTime date, IDictionary<string, object> path)
        {
            if (Scheduler != null)
            {
                await Scheduler.SelectSlot(date, date.AddMinutes(MinutesPerSlot), AppointmentsInSlot(path, date, date.AddMinutes(MinutesPerSlot)), path);
            }
        }

        async Task OnAllDaySlotClick(IDictionary<string, object> path)
        {
            if (Scheduler != null)
            {
                await Scheduler.SelectSlot(StartDate.Date, StartDate.Date.AddDays(1), AllDayAppointments(path), path);
            }
        }

        async Task OnAppointmentSelect(AppointmentData data)
        {
            if (Scheduler != null)
            {
                await Scheduler.SelectAppointment(data);
            }
        }

        IDictionary<string, object> Attributes(DateTime date, IDictionary<string, object> path, int index)
        {
            var attributes = Scheduler?.GetSlotAttributes(date, date.AddMinutes(MinutesPerSlot), () => AppointmentsInSlot(path, date, date.AddMinutes(MinutesPerSlot)), path) ?? new Dictionary<string, object>();

            var focused = !allDayFocused && date == currentSlot && index == currentResource;

            attributes["class"] = ClassList.Create("rz-slot").Add("rz-slot-minor", (date.Minute / MinutesPerSlot) % 2 == 1).Add("rz-state-focused", focused).Add(attributes).ToString();
            attributes["dropzone"] = "move";

            return attributes;
        }

        void OnFocus()
        {
            if (currentSlot == default)
            {
                currentSlot = StartDate;
                currentResource = 0;
            }
        }

        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;
            var paths = Paths;

            if (key == "ArrowUp" || key == "ArrowDown")
            {
                if (key == "ArrowUp")
                {
                    if (!allDayFocused)
                    {
                        if (currentSlot <= StartDate && ShowAllDay)
                        {
                            allDayFocused = true;
                        }
                        else if (currentSlot > StartDate)
                        {
                            currentSlot = currentSlot.AddMinutes(-MinutesPerSlot);
                        }
                    }
                }
                else
                {
                    if (allDayFocused)
                    {
                        allDayFocused = false;
                        currentSlot = StartDate;
                    }
                    else if (currentSlot.AddMinutes(MinutesPerSlot) < EndDate)
                    {
                        currentSlot = currentSlot.AddMinutes(MinutesPerSlot);
                    }
                }

                preventKeyPress = true;
                stopKeydownPropagation = true;
            }
            else if (key == "ArrowLeft" || key == "ArrowRight")
            {
                if (paths.Count > 0)
                {
                    currentResource = Math.Clamp(currentResource + (key == "ArrowLeft" ? -1 : 1), 0, paths.Count - 1);
                }

                preventKeyPress = true;
                stopKeydownPropagation = true;
            }
            else if (key == "Enter")
            {
                var path = paths.ElementAtOrDefault(currentResource);

                if (path != null)
                {
                    if (allDayFocused)
                    {
                        await OnAllDaySlotClick(path);
                    }
                    else
                    {
                        await OnSlotClick(currentSlot, path);
                    }

                    await view.FocusAsync();
                }

                preventKeyPress = true;
                stopKeydownPropagation = true;
            }
            else if (key == "Space")
            {
                var path = paths.ElementAtOrDefault(currentResource);

                if (path != null && Scheduler != null)
                {
                    var appointments = allDayFocused ? AllDayAppointments(path) : AppointmentsInSlot(path, currentSlot, currentSlot.AddMinutes(MinutesPerSlot));
                    var appointment = appointments.FirstOrDefault();

                    if (appointment != null)
                    {
                        await Scheduler.SelectAppointment(appointment);
                        await view.FocusAsync();
                    }
                }

                preventKeyPress = true;
                stopKeydownPropagation = true;
            }
            else
            {
                preventKeyPress = false;
                stopKeydownPropagation = false;
            }
        }
    }
}
