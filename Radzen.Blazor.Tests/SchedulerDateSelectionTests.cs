using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        static IRenderedComponent<RadzenDatePicker<DateTime>> OpenPicker(TestContext ctx, IRenderedComponent<RadzenScheduler<Appointment>> cut, int openCount = 1)
        {
            cut.Find(".rz-scheduler-nav-title button").Click();

            // Opening finishes by focusing the picker a moment after the popup is shown.
            cut.WaitForAssertion(() => Assert.Equal(openCount, ctx.JSInterop.Invocations["Radzen.focusElement"].Count));

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
            var picker = OpenPicker(ctx, cut);

            Assert.True(picker.Instance.ShowDays);

            cut.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(new DateTime(2027, 3, 5)));

            cut.WaitForAssertion(() => Assert.Equal(new DateTime(2027, 3, 5), cut.Instance.CurrentDate));
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
            var picker = OpenPicker(ctx, cut);

            Assert.False(picker.Instance.ShowDays);
            Assert.Equal(12, cut.FindAll(".rz-calendar-month-cell").Count);
        }

        [Fact]
        public void PickingMonth_NavigatesToItAndClosesPicker()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(ctx, cut);

            PickMonth(cut, 3);

            cut.WaitForAssertion(() =>
            {
                Assert.Equal(new DateTime(2026, 3, 14), cut.Instance.CurrentDate);
                Assert.Equal("March 2026", cut.Find(".rz-scheduler-nav-title").TextContent.Trim());
                ctx.JSInterop.VerifyInvoke("Radzen.closePopup");
            });
        }

        [Fact]
        public void PickingDisplayedMonth_ClosesPicker()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(ctx, cut);

            PickMonth(cut, 7);

            cut.WaitForAssertion(() => ctx.JSInterop.VerifyInvoke("Radzen.closePopup"));
        }

        [Theory]
        [InlineData(typeof(RadzenMonthView))]
        [InlineData(typeof(RadzenYearView))]
        public void ArrowKeys_InMonthGrid_ReachAnyMonthWithoutNavigatingOrClosing(Type viewType)
        {
            using var ctx = new TestContext();

            var cut = viewType == typeof(RadzenMonthView)
                ? RenderScheduler<RadzenMonthView>(ctx)
                : RenderScheduler<RadzenYearView>(ctx);
            OpenPicker(ctx, cut);

            // July to September with the right arrow, then three months down to December. The browser keeps focus on the active cell.
            foreach (var (key, month) in new[] { ("ArrowRight", "August"), ("ArrowRight", "September"), ("ArrowDown", "December") })
            {
                cut.Find(".rz-calendar-month-cell.rz-state-active").KeyDown(new KeyboardEventArgs { Code = key });
                cut.WaitForAssertion(() => Assert.Equal(month, cut.Find(".rz-calendar-month-cell.rz-state-active").TextContent.Trim()));
            }

            Assert.Equal(new DateTime(2026, 7, 14), cut.Instance.CurrentDate);
            ctx.JSInterop.VerifyNotInvoke("Radzen.closePopup");

            // In the browser, Enter on the focused month cell clicks it.
            cut.Find(".rz-calendar-month-cell.rz-state-active").Click();

            cut.WaitForAssertion(() =>
            {
                Assert.Equal(new DateTime(2026, 12, 14), cut.Instance.CurrentDate);
                ctx.JSInterop.VerifyInvoke("Radzen.closePopup");
            });
        }

        [Fact]
        public void PickingShownYear_ReturnsToMonthGrid()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(ctx, cut);

            cut.Find(".rz-calendar-title-button").Click();
            cut.WaitForElement("button[id$='-year-2026']").Click();

            cut.WaitForAssertion(() => Assert.Equal(12, cut.FindAll(".rz-calendar-month-cell").Count));
            Assert.Equal(new DateTime(2026, 7, 14), cut.Instance.CurrentDate);
            ctx.JSInterop.VerifyNotInvoke("Radzen.closePopup");
        }

        [Theory]
        [InlineData(typeof(RadzenMonthView), "-month-7")]
        [InlineData(typeof(RadzenWeekView), "-day-2026-07-14")]
        public void OpeningPicker_FocusesTheCurrentMonthOrDay(Type viewType, string expectedIdSuffix)
        {
            using var ctx = new TestContext();

            var cut = viewType == typeof(RadzenMonthView)
                ? RenderScheduler<RadzenMonthView>(ctx)
                : RenderScheduler<RadzenWeekView>(ctx);
            OpenPicker(ctx, cut);

            cut.WaitForAssertion(() =>
            {
                var focused = Assert.Single(ctx.JSInterop.Invocations["Radzen.focusElement"]);
                Assert.EndsWith(expectedIdSuffix, (string)focused.Arguments[0]!);
            });
        }

        [Fact]
        public void ReopeningPicker_StartsItOverAtTheCurrentDate()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(ctx, cut);
            cut.Find(".rz-calendar-next").Click();

            cut.WaitForAssertion(() => Assert.Equal("2027", cut.Find(".rz-calendar-title-text").TextContent.Trim()));

            // The title button closes the popup, and the next click opens it again.
            cut.Find(".rz-scheduler-nav-title button").Click();
            OpenPicker(ctx, cut, openCount: 2);

            cut.WaitForAssertion(() => Assert.Equal("2026", cut.Find(".rz-calendar-title-text").TextContent.Trim()));
        }

        [Fact]
        public void Escape_InCells_ReturnsFocusToTheTitle()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(ctx, cut);
            int FocusCalls() => ctx.JSInterop.Invocations.Count(i => i.Identifier == "Blazor._internal.domWrapper.focus");

            // After an arrow key the cells stop keydown from bubbling, which Escape must still get through.
            cut.Find(".rz-calendar-month-cell.rz-state-active").KeyDown(new KeyboardEventArgs { Code = "ArrowRight" });
            cut.WaitForAssertion(() => Assert.Equal("August", cut.Find(".rz-calendar-month-cell.rz-state-active").TextContent.Trim()));
            var before = FocusCalls();

            cut.Find(".rz-calendar-month-cell.rz-state-active").KeyDown(new KeyboardEventArgs { Code = "Escape" });

            cut.WaitForAssertion(() => Assert.Equal(before + 1, FocusCalls()));
        }

        [Fact]
        public void ChangingYear_OnlyBrowses_UntilAMonthIsPicked()
        {
            using var ctx = new TestContext();

            var cut = RenderScheduler<RadzenMonthView>(ctx);
            OpenPicker(ctx, cut);

            cut.Find(".rz-calendar-next").Click();

            cut.WaitForAssertion(() => Assert.Equal("2027", cut.Find(".rz-calendar-title-text").TextContent.Trim()));
            Assert.Equal(new DateTime(2026, 7, 14), cut.Instance.CurrentDate);
            ctx.JSInterop.VerifyNotInvoke("Radzen.closePopup");

            PickMonth(cut, 3);

            cut.WaitForAssertion(() => Assert.Equal(new DateTime(2027, 3, 14), cut.Instance.CurrentDate));
        }
    }
}
