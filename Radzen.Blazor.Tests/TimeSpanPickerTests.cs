using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Bunit;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TimeSpanPickerTests
    {
        const string _pickerClassSelector = ".rz-timespanpicker";
        const string _popupButtonClassSelector = ".rz-timespanpicker-trigger";
        const string _clearButtonClassSelector = ".rz-dropdown-clear-icon";
        const string _inputFieldClassSelector = ".rz-inputtext";
        const string _panelClassSelector = ".rz-timespanpicker-panel";
        static CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

        #region Component general look
        [Fact]
        public void TimeSpanPicker_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var style = "width: 200px";
            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Style, style);
            });

            var picker = component.Find(_pickerClassSelector);
            Assert.Contains(style, picker.GetStyle().CssText);
        }

        [Fact]
        public void TimeSpanPicker_DoesNotRender_Component_WhenVisibleParameterIsFalse()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Visible, false);
            });

            var pickerElements = component.FindAll(_pickerClassSelector);
            Assert.Equal(0, pickerElements.Count);
        }

        [Fact]
        public void TimeSpanPicker_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var parameterName = "autofocus";
            var parameterValue = "true";
            component.SetParametersAndRender(parameters =>
            {
                parameters.AddUnmatched(parameterName, parameterValue);
            });

            var picker = component.Find(_pickerClassSelector);
            Assert.Equal(parameterValue, picker.GetAttribute(parameterName));
        }

        [Fact]
        public void TimeSpanPicker_Renders_EmptyCssClass_WhenValueIsEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Value, null);
            });

            var picker = component.Find(_pickerClassSelector);
            Assert.Contains("rz-state-empty", picker.ClassList);
        }

        [Fact]
        public void TimeSpanPicker_DoesNotRender_EmptyCssClass_WhenValueIsNotEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var value = TimeSpan.FromHours(1);
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Value, value);
            });

            var picker = component.Find(_pickerClassSelector);
            Assert.DoesNotContain("rz-state-empty", picker.ClassList);
        }
        #endregion

        #region Input field look
        [Fact]
        public void TimeSpanPicker_Renders_InputAttributesParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var parameterName = "autofocus";
            var parameterValue = "true";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.InputAttributes, new Dictionary<string, object>(){{ parameterName, parameterValue } });
            });

            var inputField = component.Find(_inputFieldClassSelector);
            Assert.Equal(parameterValue, inputField.GetAttribute(parameterName));
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

            var inputField = component.Find(_inputFieldClassSelector);
            Assert.Contains(inputClass, inputField.ClassList);
        }

        [Fact]
        public void TimeSpanPicker_Renders_NameParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var name = "timespanpicker-test";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Name, name);
            });

            var inputField = component.Find(_inputFieldClassSelector);
            Assert.Equal(name, inputField.GetAttribute("name"));
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

            var clearButtons = component.FindAll(_clearButtonClassSelector);
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

            var inputField = component.Find(_inputFieldClassSelector);

            Assert.True(inputField.HasAttribute("readonly"));
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

            var picker = component.Find(_pickerClassSelector);
            var inputField = component.Find(_inputFieldClassSelector);

            Assert.Contains("rz-state-disabled", picker.ClassList);
            Assert.True(inputField.HasAttribute("disabled"));
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

            var inputField = component.Find(_inputFieldClassSelector);

            Assert.Contains("rz-readonly", inputField.ClassList);
            Assert.True(inputField.HasAttribute("readonly"));
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

            var triggerButtons = component.FindAll(_popupButtonClassSelector);
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
                parameters.Add(p => p.ShowPopupButton, true);
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

            var tabIndex = 15;
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.TabIndex, tabIndex);
            });

            var inputField = component.Find(_inputFieldClassSelector);
            Assert.Equal(tabIndex.ToString(_cultureInfo), inputField.GetAttribute("tabindex"));
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
            var formattedValue = value.ToString(format, _cultureInfo);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.TimeSpanFormat, format);
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.Culture, _cultureInfo);
            });

            var inputField = component.Find(_inputFieldClassSelector);
            Assert.Equal(formattedValue, inputField.GetAttribute("value"));
        }

        [Fact]
        public void TimeSpanPicker_Renders_PlaceholderParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var placeholder = "placeholder test";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Placeholder, placeholder);
            });

            var inputField = component.Find(_inputFieldClassSelector);
            Assert.Equal(placeholder, inputField.GetAttribute("placeholder"));
        }
        #endregion

        #region Input field behavior
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

            component.Find(_inputFieldClassSelector).Change(valueToSet);

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

            component.Find(_inputFieldClassSelector).Change(valueToSet);

            Assert.Equal(maxValue, component.Instance.Value);
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
                parameters.Add(p => p.Culture, _cultureInfo);
            });

            var expectedValue = new TimeSpan(15, 5, 30);
            var input = expectedValue.ToString(format, _cultureInfo);

            var inputElement = component.Find(_inputFieldClassSelector);
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

            var inputElement = component.Find(_inputFieldClassSelector);

            string input = "- 15h 5min 30s";
            TimeSpan expectedValue = new TimeSpan(15, 5, 30).Negate();

            inputElement.Change(input);

            Assert.Equal(expectedValue, component.Instance.Value);
        }

        [Fact]
        public void TimeSpanPicker_Raises_ValueChanged_OnInputChange()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            bool raised = false;
            TimeSpan newValue = TimeSpan.Zero;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; });
                parameters.Add(p => p.Culture, _cultureInfo);
            });

            TimeSpan inputValue = new TimeSpan(3, 15, 30);
            var input = inputValue.ToString(null, _cultureInfo);

            var inputElement = component.Find(_inputFieldClassSelector);

            inputElement.Change(input);

            Assert.True(raised);
            Assert.Equal(inputValue, newValue);
        }

        [Fact]
        public void TimeSpanPicker_Raises_Change_OnInputChange()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            bool raised = false;
            TimeSpan? newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
                parameters.Add(p => p.Culture, _cultureInfo);
            });

            TimeSpan inputValue = new TimeSpan(3, 15, 30);
            var input = inputValue.ToString(null, _cultureInfo);

            var inputElement = component.Find(_inputFieldClassSelector);

            inputElement.Change(input);

            Assert.True(raised);
            Assert.Equal(inputValue, newValue);
        }

        // TimeSpanPicker_Opens_Popup_OnPopupButtonClick – I don't know how to test it if I can't check if an element is visible

        [Fact]
        public void TimeSpanPicker_Raises_ChangeEvent_OnClearButtonClick()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var raised = false;
            TimeSpan? value = new TimeSpan(3, 15, 30);
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.AllowClear, true);
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.Change, args => { raised = true; value = args; });
            });

            var clearButton = component.Find(_clearButtonClassSelector);
            clearButton.Click();

            Assert.True(raised);
            Assert.Null(value);
        }
        #endregion

        #region Panel look
        [Theory]
        [InlineData(
            TimeSpanUnit.Day,
            new string[] { ".rz-timespanpicker-days" },
            new string[] { ".rz-timespanpicker-hours", ".rz-timespanpicker-minutes", ".rz-timespanpicker-seconds", ".rz-timespanpicker-milliseconds", ".rz-timespanpicker-microseconds" })
        ]
        [InlineData(
            TimeSpanUnit.Minute,
            new string[] { ".rz-timespanpicker-days", ".rz-timespanpicker-hours", ".rz-timespanpicker-minutes" },
            new string[] { ".rz-timespanpicker-seconds", ".rz-timespanpicker-milliseconds", ".rz-timespanpicker-microseconds" })
        ]
        [InlineData(
            TimeSpanUnit.Microsecond,
            new string[] { ".rz-timespanpicker-days", ".rz-timespanpicker-hours", ".rz-timespanpicker-minutes", ".rz-timespanpicker-seconds", ".rz-timespanpicker-milliseconds", ".rz-timespanpicker-microseconds" },
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
                var foundElements = component.FindAll($"{_panelClassSelector} {element}");
                Assert.Equal(1, foundElements.Count);
            }
            foreach (var element in elementsNotToRender)
            {
                var foundElements = component.FindAll($"{_panelClassSelector} {element}");
                Assert.Equal(0, foundElements.Count);
            }
        }
        #endregion
    }
}
