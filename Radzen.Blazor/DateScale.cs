using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    internal class DateScale : LinearScale
    {
        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var start = Input.Start;

            var end = Input.End;

            var step = Math.Abs(end - start) / CalculateTickCount(distance);

            if (Step != null)
            {
                if (Step is TimeSpan timeSpan)
                {
                    step = timeSpan.Ticks;
                }
            }

            if (!Double.IsFinite(start) && !Double.IsFinite(end))
            {
                start = DateTime.Now.Date.AddDays(-1).Ticks;
                end = DateTime.Now.Date.AddDays(1).Ticks;
                Step = TimeSpan.FromDays(1);
                step = TimeSpan.FromDays(1).Ticks;
            }

            if (start == end)
            {
                start = FromTicks(start).Date.AddDays(-1).Ticks;
                end = FromTicks(end).Date.AddDays(1).Ticks;
                Step = TimeSpan.FromDays(1);
                step = TimeSpan.FromDays(1).Ticks;
            }

            return (start, end, step);
        }

        public override object Value(double value)
        {
            return FromTicks(value);
        }

        public override void Resize(object min, object max)
        {
            if (min != null)
            {
                var minDate = Convert.ToDateTime(min);
                Input.Start = minDate.Ticks;
                Round = false;
            }

            if (max != null)
            {
                var maxDate = Convert.ToDateTime(max);
                Input.End = maxDate.Ticks;
                Round = false;
            }
        }

        private DateTime FromTicks(double value)
        {
            return new DateTime(Convert.ToInt64(value));
        }

        public override string FormatTick(string format, object value)
        {
            if (string.IsNullOrEmpty(format))
            {
                return ((DateTime)value).ToShortDateString();
            }

            return base.FormatTick(format, value);
        }

        public override void Fit(int distance)
        {
            var ticks = Ticks(distance);

            Input.MergeWidth(new ScaleRange { Start = ticks.Start, End = ticks.End });

            Round = false;
            Step = TimeSpan.FromTicks(Convert.ToInt64(ticks.Step));
        }
    }
}
