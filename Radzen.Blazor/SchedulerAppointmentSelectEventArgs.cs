using System;

namespace Radzen
{
    public class SchedulerAppointmentSelectEventArgs<TItem>
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TItem Data { get; set; }
    }
}