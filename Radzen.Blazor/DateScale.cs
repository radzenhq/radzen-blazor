using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    internal class DateScale : LinearScale
    {
        private object? step;

        private bool autoStep;

        public override object? Step
        {
            get => step;
            set
            {
                step = value;
                autoStep = false;
            }
        }

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

        public override IEnumerable<double> TickValues(int distance)
        {
            if (Step is TimeSpan && !autoStep)
            {
                return base.TickValues(distance);
            }

            var start = Input.Start;
            var end = Input.End;

            if (!double.IsFinite(start) || !double.IsFinite(end) || start >= end)
            {
                return base.TickValues(distance);
            }

            var rawStep = (end - start) / CalculateTickCount(distance);

            return CalendarTicks(FromTicks(start), FromTicks(end), rawStep);
        }

        private static IEnumerable<double> CalendarTicks(DateTime start, DateTime end, double rawStepTicks)
        {
            var sixMonths = TimeSpan.FromDays(183).Ticks;

            if (rawStepTicks <= TimeSpan.FromDays(14).Ticks)
            {
                var spans = new[]
                {
                    TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30),
                    TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(3), TimeSpan.FromHours(6), TimeSpan.FromHours(12),
                    TimeSpan.FromDays(1), TimeSpan.FromDays(2), TimeSpan.FromDays(7), TimeSpan.FromDays(14)
                };

                var span = spans.FirstOrDefault(s => s.Ticks >= rawStepTicks);
                if (span == default)
                {
                    span = TimeSpan.FromDays(14);
                }

                var first = new DateTime((start.Ticks + span.Ticks - 1) / span.Ticks * span.Ticks);

                for (var tick = first; tick <= end; tick = tick.Add(span))
                {
                    yield return tick.Ticks;
                }
            }
            else if (rawStepTicks <= sixMonths)
            {
                var averageMonth = TimeSpan.FromDays(30.44).Ticks;
                var monthSteps = new[] { 1, 2, 3, 6 };
                var months = monthSteps.FirstOrDefault(m => m * averageMonth >= rawStepTicks);
                if (months == 0)
                {
                    months = 6;
                }

                var first = new DateTime(start.Year, start.Month, 1);
                if (first < start)
                {
                    first = first.AddMonths(1);
                }

                for (var tick = first; tick <= end; tick = tick.AddMonths(months))
                {
                    yield return tick.Ticks;
                }
            }
            else
            {
                var averageYear = TimeSpan.FromDays(365.25).Ticks;
                var years = (int)Math.Ceiling(rawStepTicks / averageYear);
                var magnitude = (int)Math.Pow(10, Math.Floor(Math.Log10(years)));
                foreach (var factor in new[] { 1, 2, 5, 10 })
                {
                    if (factor * magnitude >= years)
                    {
                        years = factor * magnitude;
                        break;
                    }
                }

                var first = new DateTime(start.Year, 1, 1);
                if (first < start)
                {
                    first = first.AddYears(1);
                }

                first = new DateTime(first.Year / years * years, 1, 1);
                if (first < start)
                {
                    first = first.AddYears(years);
                }

                for (var tick = first; tick <= end; tick = tick.AddYears(years))
                {
                    yield return tick.Ticks;
                }
            }
        }

        public override object Value(double value)
        {
            return FromTicks(value);
        }

        public override void Resize(object min, object max)
        {
            if (min != null)
            {
                var minDate = Convert.ToDateTime(min, CultureInfo.InvariantCulture);
                Input.Start = minDate.Ticks;
                Round = false;
            }

            if (max != null)
            {
                var maxDate = Convert.ToDateTime(max, CultureInfo.InvariantCulture);
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
            var hadUserStep = Step is TimeSpan && !autoStep;

            var ticks = Ticks(distance);

            Input.MergeWidth(new ScaleRange { Start = ticks.Start, End = ticks.End });

            Round = false;
            step = TimeSpan.FromTicks(Convert.ToInt64(ticks.Step));
            autoStep = !hadUserStep;
        }
    }
}
