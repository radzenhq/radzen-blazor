using AngleSharp.Dom;
using Bunit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TimeSpanPickerTests
    {
        #region Component general
        [Fact]
        public void TimeSpanPicker_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            Assert.Contains(@$"rz-timespanpicker", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var value = "width: 200px";

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Style, value);
            });

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_Renders_EmptyCssClass_WhenValueIsEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, null));

            Assert.Contains(@$"rz-state-empty", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_DoesNotRender_EmptyCssClass_WhenValueIsNotEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var value = TimeSpan.FromHours(1);
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

            Assert.DoesNotContain(@$"rz-state-empty", component.Markup);
        }
        #endregion

        #region Input field
        [Fact]
        public void TimeSpanPicker_Respects_MinParameter_OnInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var minValue = TimeSpan.FromMinutes(-15);
            var initialValue = TimeSpan.Zero;
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Min, minValue);
                parameters.Add(p => p.Value, initialValue);
            });

            var valueToSet = TimeSpan.FromHours(-1);

            component.Find(".rz-inputtext").Change(valueToSet);

            Assert.Equal(minValue, component.Instance.Value);
        }

        [Fact]
        public void TimeSpanPicker_Respects_MaxParameter_OnInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var maxValue = TimeSpan.FromMinutes(15);
            var initialValue = TimeSpan.Zero;
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Max, maxValue);
                parameters.Add(p => p.Value, initialValue);
            });

            var valueToSet = TimeSpan.FromHours(1);

            component.Find(".rz-inputtext").Change(valueToSet);

            Assert.Equal(maxValue, component.Instance.Value);
        }

        [Fact]
        public void TimeSpanPicker_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.AddUnmatched("autofocus", "");
            });

            Assert.Contains("autofocus", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_Renders_InputClassParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var inputClass = "test-class";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.InputClass, inputClass);
            });

            var inputField = component.Find(".rz-inputtext");
            Assert.Contains(inputClass, inputField.ClassList);
        }

        [Fact]
        public void TimeSpanPicker_Renders_AllowClearParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.AllowClear, true);
                parameters.Add(p => p.Value, TimeSpan.FromDays(1));
            });

            var clearButtons = component.FindAll(".rz-dropdown-clear-icon");
            Assert.Equal(1, clearButtons.Count);
        }

        [Fact]
        public void TimeSpanPicker_Renders_AllowInputParameter_WhenFalse()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.AllowInput, false);
            });

            var inputFieldMarkup = component.Find(".rz-inputtext").ToMarkup();
            Assert.Contains("readonly", inputFieldMarkup);
        }

        [Fact]
        public void TimeSpanPicker_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            var inputFieldMarkup = component.Find(".rz-inputtext").ToMarkup();
            Assert.Contains("disabled", inputFieldMarkup);
            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_Renders_ReadOnlyParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ReadOnly, true);
            });

            var inputFieldMarkup = component.Find(".rz-inputtext").ToMarkup();
            Assert.Contains("readonly", inputFieldMarkup);
            Assert.Contains("rz-readonly", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_Renders_ShowPopupButtonParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ShowPopupButton, true);
            });

            var triggerButtons = component.FindAll(".rz-timespanpicker-trigger");
            Assert.Equal(1, triggerButtons.Count);
        }

        [Fact]
        public void TimeSpanPicker_Renders_PopupButtonClassParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var inputClass = "test-class";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.PopupButtonClass, inputClass);
            });

            var popupButton = component.Find(".rz-timespanpicker-trigger");
            Assert.Contains(inputClass, popupButton.ClassList);
        }

        [Fact]
        public void TimeSpanPicker_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_Renders_TimeSpanFormatParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var format = "d'd 'h'h 'm'min 's's'";
            var value = new TimeSpan(1, 6, 30, 15);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.TimeSpanFormat, format);
                parameters.Add(p => p.Value, value);
            });

            var formattedValue = value.ToString(format);

            Assert.Contains(@$"value=""{formattedValue}""", component.Markup);
        }

        [Fact]
        public void TimeSpanPicker_Parses_Input_Using_TimeSpanFormat()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var format = "h'-'m'-'s";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.TimeSpanFormat, format);
            });

            var expectedValue = new TimeSpan(15, 5, 30);
            var input = expectedValue.ToString(format);

            var inputElement = component.Find(".rz-inputtext");
            inputElement.Change(input);

            Assert.Equal(expectedValue, component.Instance.Value);
        }

        [Fact]
        public void TimeSpanPicker_Parses_Input_Using_ParseInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            Func<string, TimeSpan?> customParseInput = (input) =>
            {
                if (TimeSpan.TryParseExact(input, "'- 'h'h 'm'min 's's'", null, TimeSpanStyles.AssumeNegative, out var result))
                {
                    return result;
                }
                return null;
            };

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ParseInput, customParseInput);
            });

            var inputElement = component.Find(".rz-inputtext");

            string input = "- 15h 5min 30s";
            TimeSpan expectedValue = new TimeSpan(15, 5, 30).Negate();

            inputElement.Change(input);

            Assert.Equal(expectedValue, component.Instance.Value);
        }
        #endregion

        #region Panel
        [Theory]
        [InlineData(
            TimeSpanUnit.Day,
            new string[] { "rz-timespanpicker-days" },
            new string[] { "rz-timespanpicker-hours", "rz-timespanpicker-minutes", "rz-timespanpicker-seconds", "rz-timespanpicker-milliseconds", "rz-timespanpicker-microseconds" })
        ]
        [InlineData(
            TimeSpanUnit.Minute,
            new string[] { "rz-timespanpicker-days", "rz-timespanpicker-hours", "rz-timespanpicker-minutes" },
            new string[] { "rz-timespanpicker-seconds", "rz-timespanpicker-milliseconds", "rz-timespanpicker-microseconds" })
        ]
        [InlineData(
            TimeSpanUnit.Microsecond,
            new string[] { "rz-timespanpicker-days", "rz-timespanpicker-hours", "rz-timespanpicker-minutes", "rz-timespanpicker-seconds", "rz-timespanpicker-milliseconds", "rz-timespanpicker-microseconds" },
            new string[0])
        ]
        public void TimeSpanPicker_Renders_FieldPrecisionParameter(TimeSpanUnit precision, string[] elementsToRender, string[] elementsNotToRender)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FieldPrecision, precision);
            });

            foreach (var element in elementsToRender)
            {
                Assert.Contains(element, component.Markup);
            }
            foreach (var element in elementsNotToRender)
            {
                Assert.DoesNotContain(element, component.Markup);
            }
        }
        #endregion
    }
}
