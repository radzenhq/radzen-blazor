using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GanttHeaderDateFormatTests
    {
        static CultureInfo CultureWithShortDatePattern(string pattern)
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = pattern;
            return culture;
        }

        [Theory]
        [InlineData("M/d/yyyy", "M/d")]
        [InlineData("dd/MM/yyyy", "dd/MM")]
        [InlineData("dd.MM.yyyy", "dd.MM")]
        [InlineData("yyyy-MM-dd", "MM-dd")]
        [InlineData("yyyy/M/d", "M/d")]
        [InlineData("yyyy. MM. dd.", "MM. dd.")]
        [InlineData("d.M.yy", "d.M")]
        [InlineData("dd-MM-yy", "dd-MM")]
        [InlineData("d 'de' MMMM 'de' yyyy", "d 'de' MMMM")]
        [InlineData("d/M/yyyy g", "d/M")]
        public void MonthDayPattern_StripsYearFromShortDatePattern(string shortDatePattern, string expected)
        {
            var culture = CultureWithShortDatePattern(shortDatePattern);

            Assert.Equal(expected, GanttHeaderDateFormat.MonthDayPattern(culture));
        }

        [Fact]
        public void MonthDayPattern_FallsBackToCultureMonthDayPattern_WhenDayOrMonthMissing()
        {
            var culture = CultureWithShortDatePattern("yyyy");

            Assert.Equal(culture.DateTimeFormat.MonthDayPattern, GanttHeaderDateFormat.MonthDayPattern(culture));
        }

        [Fact]
        public void Resolve_PrefersExplicitHeaderDateFormat()
        {
            var culture = CultureWithShortDatePattern("M/d/yyyy");

            Assert.Equal("yyyy-MM-dd", GanttHeaderDateFormat.Resolve("yyyy-MM-dd", culture));
            Assert.Equal("M/d", GanttHeaderDateFormat.Resolve(null, culture));
            Assert.Equal("M/d", GanttHeaderDateFormat.Resolve("  ", culture));
        }

        [Fact]
        public void DayView_HourColumns_FollowCultureDayMonthOrder()
        {
            var culture = CultureWithShortDatePattern("dd/MM/yyyy");
            var format = GanttHeaderDateFormat.Resolve(null, culture);

            var columns = RadzenGanttDayView<object>.BuildHourColumns(new DateTime(2026, 7, 26), new DateTime(2026, 7, 27), 80, culture, format);

            Assert.Equal(24, columns.Count);
            Assert.All(columns, c => Assert.Equal("Sun 26/07", c.GroupLabel));
            Assert.Equal("00:00", columns[0].Label);
        }

        [Fact]
        public void DayView_HourColumns_UseHeaderDateFormat()
        {
            var culture = CultureWithShortDatePattern("M/d/yyyy");

            var columns = RadzenGanttDayView<object>.BuildHourColumns(new DateTime(2026, 7, 26), new DateTime(2026, 7, 27), 80, culture, "yyyy-MM-dd");

            Assert.Equal("Sun 2026-07-26", columns[0].GroupLabel);
        }

        [Fact]
        public void MonthView_WeekColumns_FollowCultureDayMonthOrder()
        {
            var culture = CultureWithShortDatePattern("dd/MM/yyyy");
            var format = GanttHeaderDateFormat.Resolve(null, culture);

            var columns = RadzenGanttMonthView<object>.BuildWeekColumns(new DateTime(2026, 7, 26), new DateTime(2026, 8, 9), 32, culture, format);

            Assert.Equal(2, columns.Count);
            Assert.Equal("26/07 – 01/08", columns[0].Label);
            Assert.Equal("02/08 – 08/08", columns[1].Label);
        }

        [Fact]
        public void MonthView_WeekColumns_UseHeaderDateFormat()
        {
            var culture = CultureWithShortDatePattern("M/d/yyyy");

            var columns = RadzenGanttMonthView<object>.BuildWeekColumns(new DateTime(2026, 7, 26), new DateTime(2026, 8, 2), 32, culture, "yyyy-MM-dd");

            Assert.Equal("2026-07-26 – 2026-08-01", columns[0].Label);
        }

        class Item
        {
            public int Id { get; set; }
            public string Text { get; set; } = "";
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });
            return ctx;
        }

        static IRenderedComponent<RadzenGantt<Item>> RenderGantt(TestContext ctx, GanttZoomLevel zoom, CultureInfo culture, string headerDateFormat = null)
        {
            var items = new List<Item> { new Item { Id = 1, Text = "Task", Start = new DateTime(2026, 7, 27), End = new DateTime(2026, 8, 20) } };

            return ctx.RenderComponent<RadzenGantt<Item>>(p =>
            {
                p.Add(x => x.Data, items);
                p.Add(x => x.IdProperty, nameof(Item.Id));
                p.Add(x => x.TextProperty, nameof(Item.Text));
                p.Add(x => x.StartProperty, nameof(Item.Start));
                p.Add(x => x.EndProperty, nameof(Item.End));
                p.Add(x => x.ZoomLevel, zoom);
                p.Add(x => x.Culture, culture);

                if (headerDateFormat != null)
                {
                    p.Add(x => x.HeaderDateFormat, headerDateFormat);
                }
            });
        }

        [Fact]
        public void Gantt_MonthZoom_HeaderLabelsFollowGanttCulture()
        {
            using var ctx = CreateContext();
            var culture = CultureWithShortDatePattern("dd/MM/yyyy");

            var gantt = RenderGantt(ctx, GanttZoomLevel.Month, culture);

            Assert.Contains("26/07 – 01/08", gantt.Markup);
            Assert.DoesNotContain("7/26", gantt.Markup);
        }

        [Fact]
        public void Gantt_MonthZoom_HeaderLabelsUseHeaderDateFormat()
        {
            using var ctx = CreateContext();
            var culture = CultureWithShortDatePattern("M/d/yyyy");

            var gantt = RenderGantt(ctx, GanttZoomLevel.Month, culture, "yyyy-MM-dd");

            Assert.Contains("2026-07-26 – 2026-08-01", gantt.Markup);
        }

        [Fact]
        public void Gantt_DayZoom_HeaderLabelsFollowGanttCulture()
        {
            using var ctx = CreateContext();
            var culture = CultureWithShortDatePattern("dd/MM/yyyy");

            var gantt = RenderGantt(ctx, GanttZoomLevel.Day, culture);

            Assert.Contains("Mon 27/07", gantt.Markup);
        }

        [Fact]
        public void Gantt_ZoomButtons_CarryViewClasses()
        {
            using var ctx = CreateContext();

            var gantt = RenderGantt(ctx, GanttZoomLevel.Month, CultureInfo.InvariantCulture);

            var buttons = gantt.FindAll(".rz-scheduler-nav-views button");

            Assert.Equal(5, buttons.Count);
            Assert.Contains("rz-gantt-view-day", buttons[0].ClassList);
            Assert.Contains("rz-gantt-view-week", buttons[1].ClassList);
            Assert.Contains("rz-gantt-view-month", buttons[2].ClassList);
            Assert.Contains("rz-gantt-view-year", buttons[3].ClassList);
            Assert.Contains("rz-gantt-view-years", buttons[4].ClassList);
            Assert.Contains("rz-state-active", buttons[2].ClassList);
            Assert.Equal(1, buttons.Count(b => b.ClassList.Contains("rz-state-active")));
        }
    }
}
