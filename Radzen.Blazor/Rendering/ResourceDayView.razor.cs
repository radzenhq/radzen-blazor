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
    /// <see cref="SchedulerViewBase.GroupByResource" /> is set to <c>true</c>.
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

        IList<object> Resources => Scheduler?.Resources ?? Array.Empty<object>();

        bool IsAllDay(AppointmentData appointment)
        {
            return appointment.Start <= StartDate && appointment.End >= EndDate;
        }

        IList<AppointmentData> AppointmentsForResource(object resource)
        {
            if (Appointments == null || Scheduler == null)
            {
                return Array.Empty<AppointmentData>();
            }

            return Appointments.Where(item => Scheduler.IsAppointmentInResource(item, resource) && !(ShowAllDay && IsAllDay(item))).ToList();
        }

        internal AppointmentData[] AllDayAppointments(object resource)
        {
            if (Appointments == null || Scheduler == null)
            {
                return Array.Empty<AppointmentData>();
            }

            return Appointments.Where(item => Scheduler.IsAppointmentInResource(item, resource) && IsAllDay(item)).OrderBy(item => item.Start).ThenByDescending(item => item.End).ToArray();
        }

        AppointmentData[] AppointmentsInSlot(object resource, DateTime start, DateTime end)
        {
            if (Appointments == null || Scheduler == null)
            {
                return Array.Empty<AppointmentData>();
            }

            return Appointments.Where(item => Scheduler.IsAppointmentInResource(item, resource) && Scheduler.IsAppointmentInRange(item, start, end)).OrderBy(item => item.Start).ThenByDescending(item => item.End).ToArray();
        }

        async Task OnSlotClick(DateTime date, object resource)
        {
            if (Scheduler != null)
            {
                await Scheduler.SelectSlot(date, date.AddMinutes(MinutesPerSlot), AppointmentsInSlot(resource, date, date.AddMinutes(MinutesPerSlot)), resource);
            }
        }

        async Task OnAllDaySlotClick(object resource)
        {
            if (Scheduler != null)
            {
                await Scheduler.SelectSlot(StartDate.Date, StartDate.Date.AddDays(1), AllDayAppointments(resource), resource);
            }
        }

        async Task OnAppointmentSelect(AppointmentData data)
        {
            if (Scheduler != null)
            {
                await Scheduler.SelectAppointment(data);
            }
        }

        IDictionary<string, object> Attributes(DateTime date, object resource, int index)
        {
            var attributes = Scheduler?.GetSlotAttributes(date, date.AddMinutes(MinutesPerSlot), () => AppointmentsInSlot(resource, date, date.AddMinutes(MinutesPerSlot)), resource) ?? new Dictionary<string, object>();

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
            var resourceCount = Resources.Count;

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
                if (resourceCount > 0)
                {
                    currentResource = Math.Clamp(currentResource + (key == "ArrowLeft" ? -1 : 1), 0, resourceCount - 1);
                }

                preventKeyPress = true;
                stopKeydownPropagation = true;
            }
            else if (key == "Enter")
            {
                var resource = Resources.ElementAtOrDefault(currentResource);

                if (resource != null)
                {
                    if (allDayFocused)
                    {
                        await OnAllDaySlotClick(resource);
                    }
                    else
                    {
                        await OnSlotClick(currentSlot, resource);
                    }

                    await view.FocusAsync();
                }

                preventKeyPress = true;
                stopKeydownPropagation = true;
            }
            else if (key == "Space")
            {
                var resource = Resources.ElementAtOrDefault(currentResource);

                if (resource != null && Scheduler != null)
                {
                    var appointments = allDayFocused ? AllDayAppointments(resource) : AppointmentsInSlot(resource, currentSlot, currentSlot.AddMinutes(MinutesPerSlot));
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
