using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SchedulerViewTitleTests
    {
        class Appointment
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        static IRenderedComponent<RadzenScheduler<Appointment>> RenderScheduler<TView>(TestContext ctx, DateTime date, Action<ComponentParameterCollectionBuilder<TView>>? configureView = null)
            where TView : SchedulerViewBase
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            return ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, date);
                p.Add(x => x.Data, new List<Appointment>());
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);
                p.AddChildContent<TView>(v => configureView?.Invoke(v));
            });
        }

        static string NavTitle(IRenderedComponent<RadzenScheduler<Appointment>> cut) => cut.Find(".rz-scheduler-nav-title").TextContent.Trim();

        [Fact]
        public void AgendaView_DefaultTitle_IsDateRange()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenAgendaView>(ctx, new DateTime(2026, 7, 14), v => v.Add(x => x.NumberOfDays, 7));

            Assert.Equal("07/14/2026 - 07/20/2026", NavTitle(cut));
        }

        [Fact]
        public void AgendaView_TitleFormat_FormatsStartDate()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenAgendaView>(ctx, new DateTime(2026, 7, 14), v =>
            {
                v.Add(x => x.NumberOfDays, 7);
                v.Add(x => x.TitleFormat, "{0:MMMM yyyy}");
            });

            Assert.Equal("July 2026", NavTitle(cut));
        }

        [Fact]
        public void AgendaView_TitleFormat_ReceivesLastVisibleDate()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenAgendaView>(ctx, new DateTime(2026, 7, 14), v =>
            {
                v.Add(x => x.NumberOfDays, 31);
                v.Add(x => x.TitleFormat, "{0:MMMM} - {1:MMMM yyyy}");
            });

            Assert.Equal("July - August 2026", NavTitle(cut));
        }

        [Fact]
        public void AgendaView_TitleFormatter_TakesPrecedenceOverTitleFormat()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenAgendaView>(ctx, new DateTime(2026, 7, 14), v =>
            {
                v.Add(x => x.TitleFormat, "{0:MMMM yyyy}");
                v.Add(x => x.TitleFormatter, (start, end) => $"Agenda for {start:yyyy-MM-dd}");
            });

            Assert.Equal("Agenda for 2026-07-14", NavTitle(cut));
        }

        [Fact]
        public void MonthView_TitleFormat_ReceivesMonthStartAndEnd()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx, new DateTime(2026, 7, 14), v => v.Add(x => x.TitleFormat, "{0:MM/dd} - {1:MM/dd}"));

            Assert.Equal("07/01 - 07/31", NavTitle(cut));
        }

        [Fact]
        public void WeekView_TitleFormatter_ReceivesWeekStartAndEnd()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenWeekView>(ctx, new DateTime(2026, 7, 14), v => v.Add(x => x.TitleFormatter, (start, end) => $"{start:yyyy-MM-dd}/{end:yyyy-MM-dd}"));

            Assert.Equal("2026-07-12/2026-07-18", NavTitle(cut));
        }

        [Fact]
        public void DayView_TitleFormatter_ReceivesCurrentDay()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenDayView>(ctx, new DateTime(2026, 7, 14), v => v.Add(x => x.TitleFormatter, (start, end) => $"{start:yyyy-MM-dd}|{end:yyyy-MM-dd}"));

            Assert.Equal("2026-07-14|2026-07-14", NavTitle(cut));
        }

        [Fact]
        public void YearView_TitleFormat_ReceivesYearStartAndEnd()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenYearView>(ctx, new DateTime(2026, 7, 14), v =>
            {
                v.Add(x => x.StartMonth, Month.April);
                v.Add(x => x.TitleFormat, "{0:MMM yyyy} - {1:MMM yyyy}");
            });

            Assert.Equal("Apr 2026 - Mar 2027", NavTitle(cut));
        }

        [Fact]
        public void YearView_DefaultTitle_IsUnchanged()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenYearView>(ctx, new DateTime(2026, 7, 14), v => v.Add(x => x.StartMonth, Month.April));

            Assert.Equal("2026-2027", NavTitle(cut));
        }
    }
}
