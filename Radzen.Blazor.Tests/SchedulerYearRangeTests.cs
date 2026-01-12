using Bunit;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using Radzen;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SchedulerYearRangeTests
    {
        class Appointment
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        [Fact]
        public void YearView_StartMonthJanuary_IncludesLastDaysOfYear_WhenYearStartsOnFirstDayOfWeek()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createScheduler", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 200, Height = 200 });

            // Make the first day of week Monday and use a year where Jan 1 is Monday (2024-01-01).
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Monday;

            var appointments = new List<Appointment>
            {
                new() { Start = new DateTime(2024, 12, 31), End = new DateTime(2025, 1, 1), Text = "Year end" }
            };

            var cut = ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Culture, culture);
                p.Add(x => x.Date, new DateTime(2024, 6, 1));
                p.Add(x => x.Data, appointments);
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.AddChildContent<RadzenYearView>(v => v.Add(x => x.StartMonth, Radzen.Month.January));
            });

            var view = Assert.IsType<RadzenYearView>(cut.Instance.SelectedView);

            // View should start on 2023-12-25 (one extra week above since 2024-01-01 is Monday).
            Assert.Equal(new DateTime(2023, 12, 25), view.StartDate.Date);

            // View end must include 2024-12-31 (it should extend to end-of-week containing the real year end).
            Assert.True(view.EndDate.Date >= new DateTime(2024, 12, 31), $"EndDate was {view.EndDate:yyyy-MM-dd}");
        }
    }
}


