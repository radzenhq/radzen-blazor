using System;
using System.Collections.Generic;

namespace Radzen
{
    public class SchedulerAppointmentRenderEventArgs<TItem>
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TItem Data { get; set; }
        public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
}