using Bunit;
using System;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DateRangePickerTests
    {
        [Fact]
        public void DateRangePicker_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>();

            Assert.Contains("rz-datepicker", component.Markup);
            Assert.Contains("rz-datepicker-range", component.Markup);
            Assert.Contains("rz-calendar-view", component.Markup);
        }

        [Fact]
        public void DateRangePicker_Selects_Range_With_Two_Clicks()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            DateRange emitted = null;
            var initial = new DateTime(2024, 1, 1);

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, initial);
                parameters.Add(p => p.ValueChanged, args => { emitted = args; });
            });

            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "10").ParentElement.Click());

            Assert.NotNull(emitted);
            Assert.Equal(new DateTime(2024, 1, 10), emitted.Start);
            Assert.Null(emitted.End);

            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "20").ParentElement.Click());

            Assert.Equal(new DateTime(2024, 1, 10), emitted.Start);
            Assert.Equal(new DateTime(2024, 1, 20), emitted.End);
        }

        [Fact]
        public void DateRangePicker_Restarts_Selection_When_Clicked_Date_Is_Before_Start()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            DateRange emitted = null;
            var initial = new DateTime(2024, 1, 1);

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, initial);
                parameters.Add(p => p.ValueChanged, args => { emitted = args; });
            });

            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "15").ParentElement.Click());
            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "5").ParentElement.Click());

            Assert.NotNull(emitted);
            Assert.Equal(new DateTime(2024, 1, 5), emitted.Start);
            Assert.Null(emitted.End);
        }

        [Fact]
        public void DateRangePicker_Allows_Same_Day_Range()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            DateRange emitted = null;
            var initial = new DateTime(2024, 1, 1);

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, initial);
                parameters.Add(p => p.ValueChanged, args => { emitted = args; });
            });

            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "10").ParentElement.Click());
            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "10").ParentElement.Click());

            Assert.NotNull(emitted);
            Assert.Equal(new DateTime(2024, 1, 10), emitted.Start);
            Assert.Equal(new DateTime(2024, 1, 10), emitted.End);
        }

        [Fact]
        public void DateRangePicker_Highlights_Days_Between_Start_And_End()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
                parameters.Add(p => p.Value, new DateRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 13)));
            });

            var active = component.FindAll("td span.rz-state-active");
            Assert.Equal(2, active.Count);
            Assert.Equal("10", active[0].TextContent);
            Assert.Equal("13", active[1].TextContent);

            var inRange = component.FindAll("td span.rz-calendar-range");
            Assert.Equal(2, inRange.Count);
            Assert.Equal("11", inRange[0].TextContent);
            Assert.Equal("12", inRange[1].TextContent);
        }

        [Fact]
        public void DateRangePicker_Previews_Range_On_Hover()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
            });

            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "10").ParentElement.Click());
            component.InvokeAsync(() => component.FindAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "13").ParentElement.MouseOver());

            var inRange = component.FindAll("td span.rz-calendar-range");
            Assert.Equal(2, inRange.Count);
            Assert.Equal("11", inRange[0].TextContent);
            Assert.Equal("12", inRange[1].TextContent);
        }

        [Fact]
        public void DateRangePicker_Formats_Value_With_Separator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
                parameters.Add(p => p.Value, new DateRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 20)));
            });

            var input = component.Find(".rz-inputtext");
            Assert.Equal("10/01/2024 - 20/01/2024", input.GetAttribute("value"));
        }

        [Fact]
        public void DateRangePicker_Formats_Value_With_Custom_Separator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
                parameters.Add(p => p.Separator, " → ");
                parameters.Add(p => p.Value, new DateRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 20)));
            });

            var input = component.Find(".rz-inputtext");
            Assert.Equal("10/01/2024 → 20/01/2024", input.GetAttribute("value"));
        }

        [Fact]
        public void DateRangePicker_Normalizes_Reversed_Range_Value()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
                parameters.Add(p => p.Value, new DateRange(new DateTime(2024, 1, 20), new DateTime(2024, 1, 10)));
            });

            var input = component.Find(".rz-inputtext");
            Assert.Equal("10/01/2024 - 20/01/2024", input.GetAttribute("value"));
        }

        [Fact]
        public void DateRangePicker_Clears_Value()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            DateRange emitted = new DateRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 20));
            var raised = false;

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.AllowClear, true);
                parameters.Add(p => p.Value, emitted);
                parameters.Add(p => p.ValueChanged, args => { raised = true; emitted = args; });
            });

            component.InvokeAsync(() => component.Find(".rz-dropdown-clear-icon").Click());

            Assert.True(raised);
            Assert.Null(emitted);
        }

        [Fact]
        public void DateRangePicker_Renders_Two_Months_By_Default()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
            });

            var grids = component.FindAll("table[role=grid]");
            Assert.Equal(2, grids.Count);

            var titles = component.FindAll(".rz-calendar-month-title");
            Assert.Equal(2, titles.Count);
            Assert.NotEqual(titles[0].TextContent, titles[1].TextContent);

            Assert.Empty(component.FindAll(".rz-calendar-month-dropdown"));
            Assert.Empty(component.FindAll(".rz-calendar-year-dropdown"));

            Assert.Single(component.FindAll(".rz-calendar-prev-year"));
            Assert.Single(component.FindAll(".rz-calendar-prev"));
            Assert.Single(component.FindAll(".rz-calendar-next"));
            Assert.Single(component.FindAll(".rz-calendar-next-year"));
        }

        [Fact]
        public void DateRangePicker_Navigates_By_Year_And_Month()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
            });

            var initialTitle = component.FindAll(".rz-calendar-month-title")[0].TextContent;

            component.InvokeAsync(() => component.Find(".rz-calendar-next-year").Click());
            var afterNextYear = component.FindAll(".rz-calendar-month-title")[0].TextContent;
            Assert.NotEqual(initialTitle, afterNextYear);
            Assert.Contains("2025", afterNextYear);

            component.InvokeAsync(() => component.Find(".rz-calendar-prev-year").Click());
            Assert.Equal(initialTitle, component.FindAll(".rz-calendar-month-title")[0].TextContent);

            component.InvokeAsync(() => component.Find(".rz-calendar-next").Click());
            var afterNextMonth = component.FindAll(".rz-calendar-month-title")[0].TextContent;
            Assert.NotEqual(initialTitle, afterNextMonth);

            component.InvokeAsync(() => component.Find(".rz-calendar-prev").Click());
            Assert.Equal(initialTitle, component.FindAll(".rz-calendar-month-title")[0].TextContent);
        }

        [Fact]
        public void DateRangePicker_Renders_Single_Month_When_DisplayMonths_Is_One()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.DisplayMonths, 1);
            });

            Assert.Single(component.FindAll("table[role=grid]"));
            Assert.Empty(component.FindAll(".rz-calendar-month-title"));
            Assert.Single(component.FindAll(".rz-calendar-title-button"));
            Assert.Empty(component.FindAll(".rz-calendar-month-dropdown"));
            Assert.Empty(component.FindAll(".rz-calendar-year-dropdown"));
            Assert.Empty(component.FindAll(".rz-calendar-prev-year"));
            Assert.Empty(component.FindAll(".rz-calendar-next-year"));
        }

        [Fact]
        public void DateRangePicker_Uses_DrillDown_Navigation_By_Default()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
            });

            Assert.Equal(2, component.FindAll(".rz-calendar-title-button").Count);

            component.InvokeAsync(() => component.FindAll(".rz-calendar-title-button")[0].Click());
            Assert.Equal(12, component.FindAll(".rz-calendar-month-cell").Count);
        }

        [Fact]
        public void DateRangePicker_Selects_Range_Across_Months()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            DateRange emitted = null;

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
                parameters.Add(p => p.ValueChanged, args => { emitted = args; });
            });

            component.InvokeAsync(() =>
            {
                var firstMonth = component.FindAll("table[role=grid]")[0];
                firstMonth.QuerySelectorAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "25").ParentElement.Click();
            });

            component.InvokeAsync(() =>
            {
                var secondMonth = component.FindAll("table[role=grid]")[1];
                secondMonth.QuerySelectorAll("td:not(.rz-calendar-other-month) span").First(e => e.TextContent == "5").ParentElement.Click();
            });

            Assert.NotNull(emitted);
            Assert.Equal(new DateTime(2024, 1, 25), emitted.Start);
            Assert.Equal(new DateTime(2024, 2, 5), emitted.End);
        }

        [Fact]
        public void DateRangePicker_Renders_Unique_Day_Cell_Ids()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
            });

            var ids = component.FindAll("td[id]").Select(td => td.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void DateRangePicker_Announces_Range_Endpoints_And_Selection()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
                parameters.Add(p => p.Value, new DateRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 13)));
                parameters.Add(p => p.RangeStartAriaLabel, "start of range");
                parameters.Add(p => p.RangeEndAriaLabel, "end of range");
            });

            var startCell = component.Find("td[id$='-day-2024-01-10']");
            Assert.Contains("start of range", startCell.GetAttribute("aria-label"));
            Assert.Equal("true", startCell.GetAttribute("aria-selected"));

            var endCell = component.Find("td[id$='-day-2024-01-13']");
            Assert.Contains("end of range", endCell.GetAttribute("aria-label"));
            Assert.Equal("true", endCell.GetAttribute("aria-selected"));

            var inRangeCell = component.Find("td[id$='-day-2024-01-11']");
            Assert.Equal("true", inRangeCell.GetAttribute("aria-selected"));
        }

        [Fact]
        public void DateRangePicker_DrillDown_Zooms_Out_And_Back()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.NavigationMode, DatePickerNavigationMode.DrillDown);
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
            });

            var titleButtons = component.FindAll(".rz-calendar-title-button");
            Assert.Equal(2, titleButtons.Count);

            component.InvokeAsync(() => component.FindAll(".rz-calendar-title-button")[0].Click());
            Assert.Equal(12, component.FindAll(".rz-calendar-month-cell").Count);
            Assert.Empty(component.FindAll("table[role=grid]"));
            Assert.Single(component.FindAll(".rz-calendar-title-button"));

            component.InvokeAsync(() => component.FindAll(".rz-calendar-month-cell")[5].Click());
            Assert.Equal(2, component.FindAll("table[role=grid]").Count);

            var titles = component.FindAll(".rz-calendar-title-button");
            Assert.Equal(2, titles.Count);
            Assert.Contains("2024", titles[0].TextContent);
        }

        [Fact]
        public void DateRangePicker_DropDown_Mode_Renders_Static_Titles()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.NavigationMode, DatePickerNavigationMode.DropDown);
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
            });

            Assert.Empty(component.FindAll(".rz-calendar-title-button"));
            Assert.Equal(2, component.FindAll("span.rz-calendar-month-title").Count);
        }

        [Fact]
        public void DateRangePicker_Follows_Aria_Authoring_Practices()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.InitialViewDate, new DateTime(2024, 1, 1));
                parameters.Add(p => p.Value, new DateRange(new DateTime(2024, 1, 10), new DateTime(2024, 2, 5)));
            });

            var dialog = component.Find("[role=dialog]");
            Assert.False(string.IsNullOrEmpty(dialog.GetAttribute("aria-label")));

            var trigger = component.Find(".rz-datepicker-trigger");
            Assert.Equal("dialog", trigger.GetAttribute("aria-haspopup"));
            Assert.Equal(dialog.Id, trigger.GetAttribute("aria-controls"));

            var grids = component.FindAll("table[role=grid]");
            Assert.All(grids, g => Assert.False(string.IsNullOrEmpty(g.GetAttribute("aria-label"))));

            Assert.Single(component.FindAll("td[tabindex='0']"));

            var dayCells = component.FindAll("td[id]");
            Assert.All(dayCells, c => Assert.False(string.IsNullOrEmpty(c.GetAttribute("aria-label"))));
            Assert.All(dayCells, c => Assert.Contains(c.GetAttribute("aria-selected"), new[] { "true", "false" }));

            Assert.All(component.FindAll("[aria-hidden='true']"), e => Assert.Null(e.GetAttribute("tabindex")));
            Assert.Empty(component.FindAll("[aria-pressed]"));
            Assert.Empty(component.FindAll("button:not([aria-label])").Where(b => string.IsNullOrWhiteSpace(b.TextContent)));

            component.InvokeAsync(() => component.FindAll(".rz-calendar-title-button")[0].Click());

            var monthsGroup = component.Find(".rz-calendar-months");
            Assert.Equal("group", monthsGroup.GetAttribute("role"));
            Assert.False(string.IsNullOrEmpty(monthsGroup.GetAttribute("aria-label")));

            var focusedMonth = component.FindAll(".rz-calendar-month-cell").Single(c => c.GetAttribute("tabindex") == "0");
            Assert.Equal("true", focusedMonth.GetAttribute("aria-current"));
            Assert.Contains("2024", focusedMonth.GetAttribute("aria-label"));

            component.InvokeAsync(() => component.Find(".rz-calendar-title-button").Click());

            var yearsGroup = component.Find(".rz-calendar-years");
            Assert.Equal("group", yearsGroup.GetAttribute("role"));
            Assert.False(string.IsNullOrEmpty(yearsGroup.GetAttribute("aria-label")));

            var focusedYear = component.FindAll(".rz-calendar-year-cell").Single(c => c.GetAttribute("tabindex") == "0");
            Assert.Equal("true", focusedYear.GetAttribute("aria-current"));
            Assert.Empty(component.FindAll("[aria-pressed]"));
        }

        [Fact]
        public void DateRangePicker_Does_Not_Render_TimePicker()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDateRangePicker>(parameters =>
            {
                parameters.Add(p => p.ShowTime, true);
            });

            Assert.DoesNotContain("rz-timepicker", component.Markup);
        }
    }
}
