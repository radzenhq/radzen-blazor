using Bunit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DatePickerTests
    {
        [Fact]
        public void DatePicker_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            Assert.Contains(@$"rz-datepicker", component.Markup);
            Assert.Contains(@$"rz-calendar", component.Markup);
            Assert.Contains(@$"rz-calendar-header", component.Markup);
            Assert.Contains(@$"rz-calendar-view", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_ShowTimeParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ShowTime, true));

            Assert.Contains(@$"rz-timepicker", component.Markup);
            Assert.Contains(@$"rz-hour-picker", component.Markup);
            Assert.Contains(@$"rz-minute-picker", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_ShowSecondsParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.ShowTime, true);
                parameters.Add<bool>(p => p.ShowSeconds, true);
            });

            Assert.Contains(@$"rz-timepicker", component.Markup);
            Assert.Contains(@$"rz-hour-picker", component.Markup);
            Assert.Contains(@$"rz-minute-picker", component.Markup);
            Assert.Contains(@$"rz-second-picker", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_ShowTimeOkButtonParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.ShowTime, true);
                parameters.Add<bool>(p => p.ShowTimeOkButton, true);
            });

            Assert.Contains(@$"rz-timepicker", component.Markup);
            Assert.Contains(@$"rz-hour-picker", component.Markup);
            Assert.Contains(@$"rz-minute-picker", component.Markup);
            Assert.Contains(@$"<span class=""rz-button-text"">Ok</span>", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_DateFormatParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var format = "d";

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.DateFormat, format);
                parameters.Add<object>(p => p.Value, DateTime.Now);
            });

            Assert.Contains(@$"value=""{string.Format("{0:" + format + "}", DateTime.Now)}""", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_HourFormatParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.ShowTime, true);
                parameters.Add(p => p.HourFormat, "12");
            });

            Assert.Contains(@$"rz-ampm-picker", component.Markup);
            Assert.Contains(@$"rzi-chevron-up", component.Markup);
            Assert.Contains(@$"rzi-chevron-down", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_TimeOnlyParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.ShowTime, true);
                parameters.Add<bool>(p => p.TimeOnly, true);
            });

            Assert.DoesNotContain(@$"rz-calendar-header", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_AllowClearParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<object>(p => p.Value, DateTime.Now);
                parameters.Add<bool>(p => p.AllowClear, true);
            });

            Assert.Contains(@$"<i class=""notranslate rz-dropdown-clear-icon rzi rzi-times""", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_EmptyCssClass_WhenValueIsEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, null));

            Assert.Contains(@$"rz-state-empty", component.Markup);
        }

        [Fact]
        public void DatePicker_DoesNotRender_EmptyCssClass_WhenValueIsNotEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, DateTime.Now));

            Assert.DoesNotContain(@$"rz-state-empty", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"disabled", component.Markup);
            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_ReadOnlyParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ReadOnly, value));

            Assert.Contains(@$"readonly", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void DatePicker_NotRaises_ChangeEventOnNextMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-calendar-next-icon").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DatePicker_NotRaises_ValueChangedEventOnNextMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-calendar-next-icon").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DatePicker_NotRaises_ChangeEventOnPrevMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-calendar-prev-icon").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DatePicker_NotRaises_ValueChangedEventOnPrevMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-calendar-prev-icon").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DatePicker_Raises_ValueChangedEvent_Returns_PreviousDateOnInputOnDisabledDates()
        {
            IEnumerable<DateTime> dates = new DateTime[] { DateTime.Today };
            DateTime previousDay = DateTime.Today.AddDays(-1);

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; })
                          .Add(p => p.DateRender, args => { args.Disabled = dates.Contains(args.Date); });
            });

            var inputElement = component.Find(".rz-inputtext");

            // initialize DateTimeValue
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(previousDay.ToShortDateString());
            inputElement.Change(previousDay.AddDays(-1));

            // try to enter disabled date
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(DateTime.Today.ToShortDateString());
            inputElement.Change(DateTime.Today);

            Assert.True(raised);
            Assert.Equal(previousDay, (DateTime)newValue);
        }

        [Fact]
        public void DatePicker_Clears_InputOnDisabledDates()
        {
            IEnumerable<DateTime> dates = new DateTime[] { DateTime.Today };
            DateTime previousDay = DateTime.Today.AddDays(-1);

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime?>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; })
                          .Add(p => p.DateRender, args => { args.Disabled = dates.Contains(args.Date); });
            });

            var inputElement = component.Find(".rz-inputtext");

            // initialize DateTimeValue
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(previousDay.ToShortDateString());
            inputElement.Change(previousDay.AddDays(-1));

            // try to enter disabled date
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(DateTime.Today.ToShortDateString());
            inputElement.Change(DateTime.Today);

            Assert.True(raised);
            Assert.Null(newValue);
        }

        [Fact]
        public void DatePicker_Parses_Input_Using_DateFormat()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime?>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.DateFormat, "ddMM");
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; });
            });

            var inputElement = component.Find(".rz-inputtext");

            string input = "3012";
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(input);
            inputElement.Change(input);

            Assert.True(raised);
            Assert.Equal(new DateTime(DateTime.Now.Year, 12, 30), newValue);
        }


        [Fact]
        public void DatePicker_Parses_Input_Using_ParseInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime?>>();

            Func<string, DateTime?> customParseInput = (input) =>
            {
                if (DateTime.TryParseExact(input, "ddMM", null, DateTimeStyles.None, out var result))
                {
                    return result;
                }

                return null;
            };

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ParseInput, customParseInput);
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; });
            });

            var inputElement = component.Find(".rz-inputtext");

            string input = "3012";
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(input);
            inputElement.Change(input);

            Assert.True(raised);
            Assert.Equal(new DateTime(DateTime.Now.Year, 12, 30), newValue);
        }


        [Fact]
        public void DatePicker_Respects_DateTimeMaxValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameters =>
            {
                parameters.Add(p => p.Value, DateTime.MaxValue);
            });

            Assert.Contains(DateTime.MaxValue.ToString(component.Instance.DateFormat), component.Markup);

            var exception = Record.Exception(() => component.Find(".rz-calendar-next-icon")
                                                            .Click());
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        public void DatePicker_Respects_DateTimeKind(DateTimeKind kind)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameters =>
            {
                parameters.Add(x => x.Kind, kind);
                parameters.Add(x => x.ShowTime, true);
            });

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-calendar-next-icon").Click();
            component.FindAll(".rz-button-text").First(x => x.TextContent == "Ok").Click();

            Assert.True(raised);
            Assert.Equal(kind, ((DateTime)newValue).Kind);
        }

        [Fact]
        public void DatePicker_Renders_FooterTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            string actionsTemplate = "<input type=\"button\" value=\"Test\" />";

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameters =>
            {
                parameters.Add(p => p.Value, DateTime.MinValue);
                parameters.Add(p => p.FooterTemplate, actionsTemplate);
            });

            Assert.Contains(actionsTemplate, component.Markup);
        }

        [Fact]
        public void DatePicker_Converts_DateTimeOffSet_FromUtc_ToLocal()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var valueUtc = DateTimeOffset.UtcNow;
            var kind = DateTimeKind.Local;

            var component = ctx.RenderComponent<RadzenDatePicker<DateTimeOffset>>(parameters =>
            {
                parameters.Add(p => p.Kind, kind);
                parameters.Add(p => p.Value, valueUtc);
            });

            Assert.Equal(kind, (component.Instance.Value as DateTime?)?.Kind);
            Assert.Equal(valueUtc.LocalDateTime.ToString(CultureInfo.InvariantCulture), (component.Instance.Value as DateTime?)?.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void DatePicker_Converts_DateTimeOffSet_Local_ToUtc()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var valueUtc = DateTimeOffset.Now;
            var kind = DateTimeKind.Utc;

            var component = ctx.RenderComponent<RadzenDatePicker<DateTimeOffset>>(parameters =>
            {
                parameters.Add(p => p.Kind, kind);
                parameters.Add(p => p.Value, valueUtc);
            });

            Assert.Equal(kind, (component.Instance.Value as DateTime?)?.Kind);
            Assert.Equal(valueUtc.UtcDateTime.ToString(CultureInfo.InvariantCulture), (component.Instance.Value as DateTime?)?.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void DatePicker_Displays_Calender_Icon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            Assert.Contains(@$"rzi-calendar", component.Markup);
        }

        [Fact]
        public void DatePicker_Displays_Schedule_Icon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameters =>
            {
                parameters.Add(p => p.TimeOnly, true);
            });

            Assert.Contains(@$"rzi-time", component.Markup);
        }

        [Fact]
        public void DatePicker_Supports_DateOnly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            DateOnly? dateOnly = new DateOnly(2024, 1, 31);
            DateOnly? valueChangedValue = null!;
            var component = ctx.RenderComponent<RadzenDatePicker<DateOnly?>>(parameters =>
            {
                parameters.Add(p => p.Value, dateOnly);
                parameters.Add(p => p.ValueChanged, args => { valueChangedValue = args; });
            });

            Assert.False(component.Instance.ShowTime);
            var input = component.Find("input");
            input.GetAttribute("value").MarkupMatches(dateOnly.ToString());

            // update to new value
            var inputElement = component.Find(".rz-inputtext");
            DateOnly? enteredValue = new DateOnly(2024, 2, 28);
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(enteredValue.Value.ToShortDateString());
            inputElement.Change(enteredValue);

            input.GetAttribute("value").MarkupMatches(enteredValue.ToString());
            Assert.Equal(enteredValue, component.Instance.Value);
            Assert.Equal(enteredValue, valueChangedValue);
        }

        [Fact]
        public void DatePicker_Supports_TimeOnly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            TimeOnly? timeOnly = new TimeOnly(23, 59, 59);
            TimeOnly? valueChangedValue = null!;
            var component = ctx.RenderComponent<RadzenDatePicker<TimeOnly>>(parameters =>
            {
                parameters.Add(p => p.Value, timeOnly);
                parameters.Add(p => p.ValueChanged, args => { valueChangedValue = args; });
            });

            Assert.True(component.Instance.TimeOnly);
            Assert.True(component.Instance.ShowTime);
            var input = component.Find("input");
            input.GetAttribute("value").MarkupMatches(timeOnly.ToString());

            // update to new value
            var inputElement = component.Find(".rz-inputtext");
            TimeOnly? enteredValue = new TimeOnly(1, 4, 5);
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", invocation => true).SetResult(enteredValue.Value.ToLongTimeString());
            inputElement.Change(enteredValue);

            input.GetAttribute("value").MarkupMatches(enteredValue.ToString());
            Assert.Equal(enteredValue, component.Instance.Value);
            Assert.Equal(enteredValue, valueChangedValue);
        }

        [Fact]
        public void DatePicker_ShowCalendarWeek_WeekNumberAddedInAdditionalColumn()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameter =>
            {
                parameter.Add(p => p.ShowCalendarWeek, true);
            });

            Assert.Contains(@$"rz-calendar-week-number", component.Markup);
            Assert.Equal(8, component.FindAll(".rz-calendar-view th").Count());
            // check header and week number column
            Assert.Single(component.FindAll("th.rz-datepicker-week-number"));
            Assert.Equal(6, component.FindAll("td.rz-calendar-week-number").Count());
        }

        [Fact]
        public void DatePicker_ShowCalendarWeekFalse_NoAdditionalColumn()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameter =>
            {
                parameter.Add(p => p.ShowCalendarWeek, false);
            });

            Assert.DoesNotContain(@$"rz-calendar-week-number", component.Markup);
            Assert.Equal(7, component.FindAll(".rz-calendar-view th").Count());
        }

        [Fact]
        public void DatePicker_ShowCalendarWeekWithCustomTitle_TitleCorrectlyRendered()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameter =>
            {
                parameter.Add(p => p.ShowCalendarWeek, true);
                parameter.Add(p => p.CalendarWeekTitle, "Wk");
            });

            var weekNumberHeader = component.Find(".rz-calendar-view th.rz-datepicker-week-number");
            Assert.Contains("Wk", weekNumberHeader.InnerHtml);
        }
    }
}
