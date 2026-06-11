using System;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DateScaleTests
    {
        static DateScale CreateScale(DateTime start, DateTime end, double outputSize)
        {
            return new DateScale
            {
                Input = new ScaleRange { Start = start.Ticks, End = end.Ticks },
                Output = new ScaleRange { Start = 0, End = outputSize }
            };
        }

        [Fact]
        public void TickValues_MonthlyRange_AlignsToMonthStarts()
        {
            var scale = CreateScale(new DateTime(2024, 1, 1), new DateTime(2024, 12, 1), 1400);

            var ticks = scale.TickValues(100).Select(t => new DateTime((long)t)).ToList();

            Assert.Equal(12, ticks.Count);
            Assert.All(ticks, t => Assert.Equal(1, t.Day));
            Assert.All(ticks, t => Assert.Equal(TimeSpan.Zero, t.TimeOfDay));
            // No duplicate month labels with a month-only format.
            var labels = ticks.Select(t => t.ToString("MMM")).ToList();
            Assert.Equal(labels.Count, labels.Distinct().Count());
        }

        [Fact]
        public void TickValues_MonthlyRange_FewTicks_UsesMultiMonthStep()
        {
            // Narrow output - only a few ticks fit; expect quarter steps on month boundaries.
            var scale = CreateScale(new DateTime(2024, 1, 1), new DateTime(2024, 12, 1), 400);

            var ticks = scale.TickValues(100).Select(t => new DateTime((long)t)).ToList();

            Assert.InRange(ticks.Count, 3, 6);
            Assert.All(ticks, t => Assert.Equal(1, t.Day));
        }

        [Fact]
        public void TickValues_MultiYearRange_AlignsToYearStarts()
        {
            var scale = CreateScale(new DateTime(2014, 3, 15), new DateTime(2024, 6, 1), 800);

            var ticks = scale.TickValues(100).Select(t => new DateTime((long)t)).ToList();

            Assert.True(ticks.Count > 1);
            Assert.All(ticks, t => { Assert.Equal(1, t.Month); Assert.Equal(1, t.Day); });
        }

        [Fact]
        public void TickValues_DayRange_AlignsToMidnight()
        {
            var scale = CreateScale(new DateTime(2024, 5, 3, 7, 30, 0), new DateTime(2024, 5, 13, 19, 0, 0), 1000);

            var ticks = scale.TickValues(100).Select(t => new DateTime((long)t)).ToList();

            Assert.True(ticks.Count > 1);
            Assert.All(ticks, t => Assert.Equal(TimeSpan.Zero, t.TimeOfDay));
        }

        [Fact]
        public void TickValues_AfterFit_StillUsesCalendarTicks()
        {
            // RadzenChart assigns the axis Step (null) and calls Fit on every update - the
            // Fit-computed step must not disable calendar alignment.
            var scale = CreateScale(new DateTime(2024, 1, 1), new DateTime(2024, 12, 1), 1400);
            scale.Step = null;
            scale.Fit(100);

            var ticks = scale.TickValues(100).Select(t => new DateTime((long)t)).ToList();

            Assert.All(ticks, t => Assert.Equal(1, t.Day));
            var labels = ticks.Select(t => t.ToString("MMM")).ToList();
            Assert.Equal(labels.Count, labels.Distinct().Count());
        }

        [Fact]
        public void TickValues_ExplicitStep_KeepsUniformBehavior()
        {
            var scale = CreateScale(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31), 600);
            scale.Step = TimeSpan.FromDays(10);

            var ticks = scale.TickValues(100).Select(t => new DateTime((long)t)).ToList();

            Assert.Equal(new DateTime(2024, 1, 1), ticks[0]);
            Assert.Equal(new DateTime(2024, 1, 11), ticks[1]);
        }
    }
}
