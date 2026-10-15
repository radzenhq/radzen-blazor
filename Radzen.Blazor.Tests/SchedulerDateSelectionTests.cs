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
    public class SchedulerDateSelectionTests
    {
        class Appointment
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        static IRenderedComponent<RadzenScheduler<Appointment>> RenderScheduler<TView>(TestContext ctx, bool allowDateSelection = true)
            where TView : SchedulerViewBase
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            return ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, new DateTime(2026, 7, 14));
                p.Add(x => x.Data, new List<Appointment>());
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);
                p.Add(x => x.AllowDateSelection, allowDateSelection);
                p.AddChildContent<TView>();
            });
        }

        static IRenderedComponent<RadzenDatePicker<DateTime>> OpenPicker(IRenderedComponent<RadzenScheduler<Appointment>> cut)
        {
            cut.Find(".rz-scheduler-nav-title button").Click();

            return cut.FindComponent<RadzenDatePicker<DateTime>>();
        }

        static void PickMonth(IRenderedComponent<RadzenScheduler<Appointment>> cut, int month) =>
            cut.Find($"button[id$='-month-{month}']").Click();

        [Fact]
        public void Title_IsPlainText_ByDefault()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx, allowDateSelection: false);

            Assert.Empty(cut.FindAll(".rz-scheduler-nav-title button"));
            Assert.Equal("July 2026", cut.Find(".rz-scheduler-nav-title").TextContent.Trim());
        }

        [Fact]
        public void Title_IsButton_WhenAllowDateSelection()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);

            Assert.Equal("July 2026", cut.Find(".rz-scheduler-nav-title button").TextContent.Trim());
        }

        [Fact]
        public void DayBasedViews_PickDays()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenWeekView>(ctx);
            var picker = OpenPicker(cut);

            Assert.True(picker.Instance.ShowDays);

            cut.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(new DateTime(2027, 3, 5)));

            Assert.Equal(new DateTime(2027, 3, 5), cut.Instance.CurrentDate);
        }

        [Theory]
        [InlineData(typeof(RadzenMonthView))]
        [InlineData(typeof(RadzenYearView))]
        public void MonthAndYearViews_PickMonths(Type viewType)
        {
            using var ctx = new TestContext();

            var cut = viewType == typeof(RadzenMonthView)
                ? RenderScheduler<RadzenMonthView>(ctx)
                : RenderScheduler<RadzenYearView>(ctx);
            var picker = OpenPicker(cut);

            Assert.False(picker.Instance.ShowDays);
            Assert.Equal(12, cut.FindAll(".rz-calendar-month-cell").Count);
        }

        [Fact]
        public void PickingMonth_NavigatesToItAndClosesPicker()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(cut);

            PickMonth(cut, 3);

            Assert.Equal(new DateTime(2026, 3, 14), cut.Instance.CurrentDate);
            Assert.Equal("March 2026", cut.Find(".rz-scheduler-nav-title").TextContent.Trim());
            ctx.JSInterop.VerifyInvoke("Radzen.closePopup");
        }

        [Fact]
        public void PickingDisplayedMonth_ClosesPicker()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(cut);

            PickMonth(cut, 7);

            ctx.JSInterop.VerifyInvoke("Radzen.closePopup");
        }

        [Fact]
        public void ChangingYear_OnlyBrowses_UntilAMonthIsPicked()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(cut);

            cut.Find(".rz-calendar-next").Click();

            Assert.Equal(new DateTime(2026, 7, 14), cut.Instance.CurrentDate);
            ctx.JSInterop.VerifyNotInvoke("Radzen.closePopup");

            PickMonth(cut, 3);

            Assert.Equal(new DateTime(2027, 3, 14), cut.Instance.CurrentDate);
        }
    }
}
