using Bunit;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
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

            Assert.Contains(@$"rz-calendar", component.Markup);
            Assert.Contains(@$"rz-datepicker-group", component.Markup);
            Assert.Contains(@$"rz-datepicker-header", component.Markup);
            Assert.Contains(@$"rz-datepicker-calendar", component.Markup);
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

            component.SetParametersAndRender(parameters => { 
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

            component.SetParametersAndRender(parameters => {
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

            component.SetParametersAndRender(parameters => {
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

            component.SetParametersAndRender(parameters => {
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

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.ShowTime, true);
                parameters.Add<bool>(p => p.TimeOnly, true);
            });

            Assert.DoesNotContain(@$"rz-datepicker-header", component.Markup);
        }

        [Fact]
        public void DatePicker_Renders_AllowClearParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            component.SetParametersAndRender(parameters => {
                parameters.Add<object>(p => p.Value, DateTime.Now);
                parameters.Add<bool>(p => p.AllowClear, true); 
            });

            Assert.Contains(@$"<i class=""rz-dropdown-clear-icon rzi rzi-times""", component.Markup);
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

            Assert.Contains(@$"style=""display: inline-block;{value}""", component.Markup);
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
        public void DatePicker_Raises_ChangeEventOnNextMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-datepicker-next-icon").Click();

            Assert.True(raised);
            Assert.True(((DateTime)newValue) > DateTime.Now);
        }

        [Fact]
        public void DatePicker_Raises_ValueChangedEventOnNextMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-datepicker-next-icon").Click();

            Assert.True(raised);
            Assert.True(((DateTime)newValue) > DateTime.Now);
        }

        [Fact]
        public void DatePicker_Raises_ChangeEventOnPrevMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-datepicker-prev-icon").Click();

            Assert.True(raised);
            Assert.True(((DateTime)newValue) < DateTime.Now);
        }

        [Fact]
        public void DatePicker_Raises_ValueChangedEventOnPrevMonth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>();

            var raised = false;
            object newValue = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-datepicker-prev-icon").Click();

            Assert.True(raised);
            Assert.True(((DateTime)newValue) < DateTime.Now);
        }
    }
}
