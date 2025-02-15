using AngleSharp.Css.Dom;
using Bunit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TimeSpanPickerTests
    {
        const string _pickerSelector = ".rz-timespanpicker";
        const string _popupButtonSelector = $".rz-timespanpicker-trigger";
        const string _clearButtonSelector = $"{_pickerSelector} > .rz-dropdown-clear-icon";
        const string _inputFieldSelector = $"{_pickerSelector} > .rz-inputtext";
        const string _panelPopupContainerSelector = ".rz-timespanpicker-popup-container";
        const string _panelSelector = ".rz-timespanpicker-panel";
        const string _confirmationButtonSelector = ".rz-timespanpicker-confirmationbutton";
        static readonly Dictionary<TimeSpanUnit, string> _unitElementSelectors = new()
        {
            { TimeSpanUnit.Day, ".rz-timespanpicker-days" },
            { TimeSpanUnit.Hour, ".rz-timespanpicker-hours" },
            { TimeSpanUnit.Minute, ".rz-timespanpicker-minutes" },
            { TimeSpanUnit.Second, ".rz-timespanpicker-seconds" },
            { TimeSpanUnit.Millisecond, ".rz-timespanpicker-milliseconds" },
            { TimeSpanUnit.Microsecond, ".rz-timespanpicker-microseconds" }
        };
        const string _unitValuePickerSelector = ".rz-timespanpicker-unitvaluepicker";
        const string _signPickerSelector = ".rz-timespanpicker-signpicker";
        const string _positiveSignPickerSelector = $"{_signPickerSelector} > .rz-button:first-child";
        const string _negativeSignPickerSelector = $"{_signPickerSelector} > .rz-button:last-child";

        static readonly CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

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

            var picker = component.Find(_pickerSelector);
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

            var pickerElements = component.FindAll(_pickerSelector);
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

            var picker = component.Find(_pickerSelector);
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

            var picker = component.Find(_pickerSelector);
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

            var picker = component.Find(_pickerSelector);
            Assert.DoesNotContain("rz-state-empty", picker.ClassList);
        }
        #endregion

        #region Component general behavior //TO DO

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
                parameters.Add(p => p.InputAttributes, new Dictionary<string, object>() { { parameterName, parameterValue } });
            });

            var inputField = component.Find(_inputFieldSelector);
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

            var inputField = component.Find(_inputFieldSelector);
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

            var inputField = component.Find(_inputFieldSelector);
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

            var clearButtons = component.FindAll(_clearButtonSelector);
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

            var inputField = component.Find(_inputFieldSelector);

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

            var picker = component.Find(_pickerSelector);
            var inputField = component.Find(_inputFieldSelector);

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

            var inputField = component.Find(_inputFieldSelector);

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

            var triggerButtons = component.FindAll(_popupButtonSelector);
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

            var inputField = component.Find(_inputFieldSelector);
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

            var inputField = component.Find(_inputFieldSelector);
            Assert.Equal(formattedValue, inputField.GetAttribute("value"));
        }
        #endregion

        #region Input field texts
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

            var inputField = component.Find(_inputFieldSelector);
            Assert.Equal(placeholder, inputField.GetAttribute("placeholder"));
        }

        [Fact]
        public void TimeSpanPicker_Renders_TogglePopupAriaLabelParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var ariaLabel = "aria label test";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ShowPopupButton, true);
                parameters.Add(p => p.TogglePopupAriaLabel, ariaLabel);
            });

            var inputField = component.Find(_popupButtonSelector);
            Assert.Equal(ariaLabel, inputField.GetAttribute("aria-label"));
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

            component.Find(_inputFieldSelector).Change(valueToSet);

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

            component.Find(_inputFieldSelector).Change(valueToSet);

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

            var inputElement = component.Find(_inputFieldSelector);
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

            var inputElement = component.Find(_inputFieldSelector);

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

            var inputValue = new TimeSpan(3, 15, 30);
            var input = inputValue.ToString(null, _cultureInfo);

            var inputElement = component.Find(_inputFieldSelector);

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

            var inputValue = new TimeSpan(3, 15, 30);
            var input = inputValue.ToString(null, _cultureInfo);

            var inputElement = component.Find(_inputFieldSelector);

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

            var clearButton = component.Find(_clearButtonSelector);
            clearButton.Click();

            Assert.True(raised);
            Assert.Null(value);
        }
        #endregion

        #region Panel look
        [Fact]
        public void TimeSpanPicker_Renders_SignButtons_WhenMinNegative_WhenMaxPositive()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var min = TimeSpan.FromDays(-1);
            var max = TimeSpan.FromDays(1);
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
            });

            var signPickers = component.FindAll(_signPickerSelector);

            Assert.Equal(1, signPickers.Count);
        }

        public static TheoryData<int, int> TimeSpanPicker_DoesNotRender_SignButtons_WhenMinAndMaxHaveSameSignOrZero_Data
            => new()
            {
                {-10, -1},
                {-10, 0},
                {1, 10},
                {0, 10}
            };
        [Theory]
        [MemberData(nameof(TimeSpanPicker_DoesNotRender_SignButtons_WhenMinAndMaxHaveSameSignOrZero_Data))]
        public void TimeSpanPicker_DoesNotRender_SignButtons_WhenMinAndMaxHaveSameSignOrZero(int minDays, int maxDays)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var min = TimeSpan.FromDays(minDays);
            var max = TimeSpan.FromDays(maxDays);
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
            });

            var signPickers = component.FindAll(_signPickerSelector);

            Assert.Equal(0, signPickers.Count);
        }

        [Fact]
        public void TimeSpanPicker_Renders_PopupRenderModeParameter_WhenInitial()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Inline, false);
                parameters.Add(p => p.PopupRenderMode, PopupRenderMode.Initial);
            });

            var popupContainer = component.Find(_panelPopupContainerSelector);
            var panels = component.FindAll(_panelSelector);

            Assert.Equal(1, panels.Count);
        }

        [Fact]
        public void TimeSpanPicker_Renders_PopupRenderModeParameter_WhenOnDemand()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Inline, false);
                parameters.Add(p => p.PopupRenderMode, PopupRenderMode.OnDemand);
            });

            var popupContainer = component.Find(_panelPopupContainerSelector);

            Assert.False(popupContainer.HasChildNodes);
        }

        [Fact]
        public void TimeSpanPicker_Renders_InlineParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Inline, true);
            });

            var inputFields = component.FindAll(_inputFieldSelector);
            var popupContainers = component.FindAll(_panelPopupContainerSelector);
            var panels = component.FindAll(_panelSelector);

            Assert.Equal(0, inputFields.Count);
            Assert.Equal(0, popupContainers.Count);
            Assert.Equal(1, panels.Count);
        }
        #endregion

        #region Panel texts
        [Fact]
        public void TimeSpanPicker_Renders_ShowConfirmationButtonParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ShowConfirmationButton, true);
            });

            var confirmationButtons = component.FindAll(_confirmationButtonSelector);

            Assert.Equal(1, confirmationButtons.Count);
        }

        public static TheoryData<TimeSpanUnit, string> TimeSpanPicker_Renders_PadTimeValuesParameter_Data
            => new()
            {
                { TimeSpanUnit.Hour, "00"},
                { TimeSpanUnit.Minute, "00"},
                { TimeSpanUnit.Second, "00"},
                { TimeSpanUnit.Millisecond, "000"},
                { TimeSpanUnit.Microsecond, "000"}
            };
        [Theory]
        [MemberData(nameof(TimeSpanPicker_Renders_PadTimeValuesParameter_Data))]
        public void TimeSpanPicker_Renders_PadTimeValuesParameter(TimeSpanUnit unit, string format)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();
            var number = 5;
            var value = new TimeSpan(number, number, number, number, number, number);
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.PadTimeValues, true);
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.FieldPrecision, TimeSpanUnit.Microsecond);
                parameters.Add(p => p.Culture, _cultureInfo);
            });

            var formattedNumber = number.ToString(format, _cultureInfo);

            var field = component.Find($"{_unitElementSelectors[unit]} > {_unitValuePickerSelector}");

            Assert.Contains($"value=\"{formattedNumber}\"", field.ToMarkup());
        }

        public static IEnumerable<object[]> TimeSpanPicker_Renders_FieldPrecisionParameter_Data
            => Enum.GetValues<TimeSpanUnit>().Select(x => new object[] { x });
        [Theory]
        [MemberData(nameof(TimeSpanPicker_Renders_FieldPrecisionParameter_Data))]
        public void TimeSpanPicker_Renders_FieldPrecisionParameter(TimeSpanUnit precision)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FieldPrecision, precision);
            });

            foreach (var unitSelectorPair in _unitElementSelectors)
            {
                var foundElements = component.FindAll($"{_panelSelector} {unitSelectorPair.Value}");
                var expectedNumberOfElements = unitSelectorPair.Key <= precision ? 1 : 0;
                Assert.Equal(expectedNumberOfElements, foundElements.Count);
            }
        }

        [Fact]
        public void TimeSpanPicker_Renders_ConfirmationButtonTextParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();

            var label = "confirmation Test";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ShowConfirmationButton, true);
                parameters.Add(p => p.ConfirmationButtonText, label);
            });

            var confirmationButton = component.Find(_confirmationButtonSelector);

            Assert.Contains(label, confirmationButton.ToMarkup());
        }

        [Fact]
        public void TimeSpanPicker_Renders_SignButtonTextParemeters()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var min = TimeSpan.FromDays(-1);
            var max = TimeSpan.FromDays(1);
            var positiveText = "positive + Test";
            var negativeText = "negative - Test";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.PositiveButtonText, positiveText);
                parameters.Add(p => p.NegativeButtonText, negativeText);
            });

            var positiveSignPicker = component.Find(_positiveSignPickerSelector);
            var negativeSignPicker = component.Find(_negativeSignPickerSelector);

            Assert.Contains(positiveText, positiveSignPicker.ToMarkup());
            Assert.Contains(negativeText, negativeSignPicker.ToMarkup());
        }

        [Fact]
        public void TimeSpanPicker_Renders_PositiveValueTextParemeter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var value = TimeSpan.FromDays(1);
            var min = TimeSpan.FromDays(0);
            var max = TimeSpan.FromDays(10);
            string text = "positive + Test";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.PositiveValueText, text);
            });

            var panel = component.Find(_panelSelector);

            Assert.Contains(text, panel.ToMarkup());
        }

        [Fact]
        public void TimeSpanPicker_Renders_NegativeValueTextParemeter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan?>>();

            var value = TimeSpan.FromDays(-1);
            var min = TimeSpan.FromDays(-10);
            var max = TimeSpan.FromDays(0);
            string text = "negative - Test";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
                parameters.Add(p => p.NegativeValueText, text);
            });

            var panel = component.Find(_panelSelector);

            Assert.Contains(text, panel.ToMarkup());
        }

        private static void TimeSpanPicker_Renders_UnitTextParameter(
            Expression<Func<RadzenTimeSpanPicker<TimeSpan>, string>> unitTextParameterSelector, TimeSpanUnit unit)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var unitText = "unit Test 123";
            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(unitTextParameterSelector, unitText);
                parameters.Add(p => p.FieldPrecision, TimeSpanUnit.Microsecond);
            });

            var unitValuePicker = component.Find(_unitElementSelectors[unit]);
            Assert.Contains(unitText, unitValuePicker.ToMarkup());
        }
        [Fact]
        public void TimeSpanPicker_Renders_DaysUnitTextParameter()
            => TimeSpanPicker_Renders_UnitTextParameter(x => x.DaysUnitText, TimeSpanUnit.Day);
        [Fact]
        public void TimeSpanPicker_Renders_HoursUnitTextParameter()
            => TimeSpanPicker_Renders_UnitTextParameter(x => x.HoursUnitText, TimeSpanUnit.Hour);
        [Fact]
        public void TimeSpanPicker_Renders_MinutesUnitTextParameter()
            => TimeSpanPicker_Renders_UnitTextParameter(x => x.MinutesUnitText, TimeSpanUnit.Minute);
        [Fact]
        public void TimeSpanPicker_Renders_SecondsUnitTextParameter()
            => TimeSpanPicker_Renders_UnitTextParameter(x => x.SecondsUnitText, TimeSpanUnit.Second);
        [Fact]
        public void TimeSpanPicker_Renders_MillisecondsUnitTextParameter()
            => TimeSpanPicker_Renders_UnitTextParameter(x => x.MillisecondsUnitText, TimeSpanUnit.Millisecond);
        [Fact]
        public void TimeSpanPicker_Renders_MicrosecondsUnitTextParameter()
            => TimeSpanPicker_Renders_UnitTextParameter(x => x.MicrosecondsUnitText, TimeSpanUnit.Microsecond);

        #endregion

        #region Panel behavior
        private static void TimeSpanPicker_Respects_StepParameter(
            Expression<Func<RadzenTimeSpanPicker<TimeSpan>, string>> stepParameterSelector, TimeSpanUnit unit, long ticksPerUnit)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTimeSpanPicker<TimeSpan>>();
            var step = 5;
            var stepTicks = step * ticksPerUnit;
            var initialValue = new TimeSpan(1, 2, 3, 4, 5, 6);
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(stepParameterSelector, step.ToString());
                parameters.Add(p => p.Value, initialValue);
                parameters.Add(p => p.FieldPrecision, TimeSpanUnit.Microsecond);
            });

            var unitSelector = _unitElementSelectors[unit];
            var expectedValueUp = initialValue.Add(new TimeSpan(2 * stepTicks));
            var fieldUpButton = component.Find($"{unitSelector} > {_unitValuePickerSelector} > .rz-numeric-up");
            fieldUpButton.Click();
            fieldUpButton.Click();
            Assert.Equal(expectedValueUp, component.Instance.Value);

            var expectedValueDown = expectedValueUp.Add(new TimeSpan(-stepTicks));
            var fieldDownButton = component.Find($"{unitSelector} > {_unitValuePickerSelector} > .rz-numeric-down");
            fieldDownButton.Click();
            Assert.Equal(expectedValueDown, component.Instance.Value);
        }
        [Fact]
        public void TimeSpanPicker_Respects_DaysStepParameter()
            => TimeSpanPicker_Respects_StepParameter(x => x.DaysStep, TimeSpanUnit.Day, TimeSpan.TicksPerDay);
        [Fact]
        public void TimeSpanPicker_Respects_HoursStepParameter()
            => TimeSpanPicker_Respects_StepParameter(x => x.HoursStep, TimeSpanUnit.Hour, TimeSpan.TicksPerHour);
        [Fact]
        public void TimeSpanPicker_Respects_MinutesStepParameter()
            => TimeSpanPicker_Respects_StepParameter(x => x.MinutesStep, TimeSpanUnit.Minute, TimeSpan.TicksPerMinute);
        [Fact]
        public void TimeSpanPicker_Respects_SecondsStepParameter()
            => TimeSpanPicker_Respects_StepParameter(x => x.SecondsStep, TimeSpanUnit.Second, TimeSpan.TicksPerSecond);
        [Fact]
        public void TimeSpanPicker_Respects_MillisecondsStepParameter()
            => TimeSpanPicker_Respects_StepParameter(x => x.MillisecondsStep, TimeSpanUnit.Millisecond, TimeSpan.TicksPerMillisecond);
        [Fact]
        public void TimeSpanPicker_Respects_MicrosecondsStepParameter()
            => TimeSpanPicker_Respects_StepParameter(x => x.MicrosecondsStep, TimeSpanUnit.Microsecond, TimeSpan.TicksPerMicrosecond);
        #endregion
    }
}
