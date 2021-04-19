using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    public class AppointmentData
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Text { get; set; }
        public object Data { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AppointmentData data &&
                   Start == data.Start &&
                   End == data.End &&
                   Text == data.Text &&
                   EqualityComparer<object>.Default.Equals(Data, data.Data);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End, Text, Data);
        }
    }
}