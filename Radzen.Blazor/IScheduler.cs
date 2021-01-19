using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public interface IScheduler
    {
        IEnumerable<AppointmentData> GetAppointmentsInRange(DateTime start, DateTime end);
        bool IsAppointmentInRange(AppointmentData item, DateTime start, DateTime end);
        Task AddView(ISchedulerView view);
        void RemoveView(ISchedulerView view);
        bool IsSelected(ISchedulerView view);
        DateTime CurrentDate { get; set; }
        Task SelectAppointment(AppointmentData data);
        Task SelectSlot(DateTime start, DateTime end);
        IDictionary<string, object> GetAppointmentAttributes(AppointmentData item);
        RenderFragment RenderAppointment(AppointmentData item);
        Task Reload();
        double Height { get; }
    }
}