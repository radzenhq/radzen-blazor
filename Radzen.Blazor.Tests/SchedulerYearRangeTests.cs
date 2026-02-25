using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using Radzen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
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

        [Fact]
        public void YearView_LoadData_ReceivesCorrectDates_WhenStartMonthChanges()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createScheduler", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            var loadDataCalls = new List<SchedulerLoadDataEventArgs>();
            var startMonth = Month.January;
            var appointments = new List<Appointment>();

            RenderFragment childContent = builder =>
            {
                builder.OpenComponent<RadzenYearPlannerView>(0);
                builder.AddAttribute(1, nameof(RadzenYearPlannerView.StartMonth), startMonth);
                builder.CloseComponent();
            };

            var cut = ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Data, appointments);
                p.Add(x => x.Date, new DateTime(2024, 8, 1));
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);
                p.Add(x => x.LoadData, EventCallback.Factory.Create<SchedulerLoadDataEventArgs>(
                    new object(), (SchedulerLoadDataEventArgs args) =>
                    {
                        loadDataCalls.Add(new SchedulerLoadDataEventArgs { Start = args.Start, End = args.End });
                    }));
                p.Add(x => x.ChildContent, childContent);
            });

            Assert.NotEmpty(loadDataCalls);
            loadDataCalls.Clear();

            startMonth = Month.July;
            cut.SetParametersAndRender(p =>
            {
                p.Add(x => x.Data, appointments);
                p.Add(x => x.Date, new DateTime(2024, 8, 1));
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);
                p.Add(x => x.LoadData, EventCallback.Factory.Create<SchedulerLoadDataEventArgs>(
                    new object(), (SchedulerLoadDataEventArgs args) =>
                    {
                        loadDataCalls.Add(new SchedulerLoadDataEventArgs { Start = args.Start, End = args.End });
                    }));
                p.Add(x => x.ChildContent, childContent);
            });

            Assert.NotEmpty(loadDataCalls);
            var reloadStart = loadDataCalls[^1].Start;

            // With StartMonth = July and CurrentDate = 2024-08-01:
            //   yearStart = 2024-07-01, viewStart is in late June 2024
            // With the old (buggy) code, Reload fired BEFORE properties were updated,
            // so LoadData would still receive January-based dates (viewStart in late Dec 2023).
            Assert.True(reloadStart.Year == 2024 && reloadStart.Month >= 6,
                $"Expected LoadData Start to reflect July-based year range, but got {reloadStart:yyyy-MM-dd}");
        }
    }
}


