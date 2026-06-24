using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using Radzen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SchedulerMonthViewTests
    {
        class Appointment
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        [Fact]
        public void MonthView_MultiDayEvent_StartingLaterInDay_RendersFromFirstDay_WhenOtherEventsStartEarlier()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            var culture = CultureInfo.InvariantCulture;

            var monday = new DateTime(2024, 12, 2);
            var appointments = new List<Appointment>
            {
                new() { Start = monday.AddHours(8),  End = monday.AddHours(9),  Text = "Early A" },
                new() { Start = monday.AddHours(9),  End = monday.AddHours(10), Text = "Early B" },
                new() { Start = monday.AddHours(10), End = monday.AddHours(11), Text = "Early C" },
                new() { Start = monday.AddHours(12), End = monday.AddDays(2).AddHours(17), Text = "Vacation" },
            };

            var cut = ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Culture, culture);
                p.Add(x => x.Date, new DateTime(2024, 12, 9));
                p.Add(x => x.Data, appointments);
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);
                p.AddChildContent<RadzenMonthView>(v => v.Add(x => x.MaxAppointmentsInSlot, 3));
            });

            var vacation = cut.FindAll(".rz-event")
                .FirstOrDefault(el => (el.QuerySelector(".rz-event-content")?.TextContent ?? "").Contains("Vacation"));
            Assert.NotNull(vacation);

            var style = vacation!.GetAttribute("style") ?? "";

            var leftMatch = Regex.Match(style, @"inset-inline-start:\s*([\d.]+)%");
            Assert.True(leftMatch.Success, $"Could not find inset-inline-start in style: {style}");
            var left = double.Parse(leftMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            var widthMatch = Regex.Match(style, @"width:\s*([\d.]+)%");
            Assert.True(widthMatch.Success, $"Could not find width in style: {style}");
            var width = double.Parse(widthMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            var slotWidth = 100.0 / 7;
            Assert.Equal(slotWidth, left, 1);
            Assert.Equal(3 * slotWidth, width, 1);
        }

        [Fact]
        public void MonthView_Localizes_ViewStrings_Using_Scheduler_UICulture()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            var monday = new DateTime(2024, 12, 2);
            var appointments = new List<Appointment>
            {
                new() { Start = monday.AddHours(8),  End = monday.AddHours(9),  Text = "A" },
                new() { Start = monday.AddHours(9),  End = monday.AddHours(10), Text = "B" },
                new() { Start = monday.AddHours(10), End = monday.AddHours(11), Text = "C" },
                new() { Start = monday.AddHours(12), End = monday.AddHours(13), Text = "D" },
            };

            var cut = ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.UICulture, new CultureInfo("de"));
                p.Add(x => x.Date, new DateTime(2024, 12, 9));
                p.Add(x => x.Data, appointments);
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);
                p.AddChildContent<RadzenMonthView>(v => v.Add(x => x.MaxAppointmentsInSlot, 3));
            });

            // MonthView_MoreText (de) = "+ {0} weitere"; the view must localize via the scheduler's UICulture.
            Assert.Contains("weitere", cut.Markup);
            Assert.DoesNotContain("more", cut.Markup);
        }
    }
}
