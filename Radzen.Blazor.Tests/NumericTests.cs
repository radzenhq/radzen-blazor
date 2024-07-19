using System;
using Bunit;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class NumericTests
    {
        [Fact]
        public void Numeric_Renders_CssClasses()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            component.Render();

            Assert.Contains(@$"rz-numeric", component.Markup);
            Assert.Contains(@$"rz-numeric-up", component.Markup);
            Assert.Contains(@$"rz-numeric-down", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_ValueParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var value = 3.5;

            component.SetParametersAndRender(parameters => parameters.Add<double>(p => p.Value, value));

            Assert.Contains(@$"value=""{value}""", component.Markup);
        }

        [Fact]
        public void Numeric_Respect_MinParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var minValue = 2;
            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; }));
                parameters.Add<double>(p => p.Value, minValue);
                parameters.Add<decimal?>(p => p.Min, minValue);
            });

            component.Find(".rz-numeric-down").Click();

            Assert.False(raised, $"Numeric value should Change event if value is less than min value.");
        }

        [Fact]
        public void Numeric_Respect_Nullable_With_MinParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<double?>>();

            var raised = false;
            var value = 3.5;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
                parameters.Add<decimal?>(p => p.Min, 1);
            });

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));

            component.Find("input").Change("");

            Assert.True(raised);
            Assert.True(object.Equals(null, newValue));
        }

        [Fact]
        public void Numeric_Respect_MaxParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var maxValue = 10;
            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; }));
                parameters.Add<double>(p => p.Value, maxValue);
                parameters.Add<decimal?>(p => p.Max, maxValue);
            });

            component.Find(".rz-numeric-up").Click();

            Assert.False(raised, $"Numeric value should Change event if value is less than min value.");
        }

        [Fact]
        public void Numeric_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }


        [Fact]
        public void Numeric_Renders_NameParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"name=""{value}""", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_PlaceholderParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Placeholder, value));

            Assert.Contains(@$"placeholder=""{value}""", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"disabled", component.Markup);
            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_ReadOnlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ReadOnly, value));

            Assert.Contains(@$"readonly", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_AutoCompleteParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AutoComplete, false));

            Assert.Contains(@$"autocomplete=""off""", component.Markup);
            Assert.Contains(@$"aria-autocomplete=""none""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AutoComplete, true));

            Assert.Contains(@$"autocomplete=""on""", component.Markup);
            Assert.DoesNotContain(@$"aria-autocomplete", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autocomplete", "custom"));

            Assert.Contains(@$"autocomplete=""custom""", component.Markup);
            Assert.DoesNotContain(@$"aria-autocomplete", component.Markup);

            component.Instance.DefaultAutoCompleteAttribute = "autocomplete-custom";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.AutoComplete, false));

            Assert.Contains(@$"autocomplete=""autocomplete-custom""", component.Markup);
            Assert.Contains(@$"aria-autocomplete=""none""", component.Markup);
        }

        [Fact]
        public void Numeric_Renders_TypedAutoCompleteParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AutoComplete, false));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.On));

            Assert.Contains(@$"autocomplete=""off""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AutoComplete, true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.Off));

            Assert.Contains(@$"autocomplete=""off""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AutoComplete, true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.BdayMonth));

            Assert.Contains(@$"autocomplete=""{AutoCompleteType.BdayMonth.GetAutoCompleteValue()}""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AutoComplete, true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.BdayYear));

            Assert.Contains(@$"autocomplete=""{AutoCompleteType.BdayYear.GetAutoCompleteValue()}""", component.Markup);
        }

        [Fact]
        public void Numeric_Raises_ChangedEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var raised = false;
            var value = 3.5;
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; newValue = args; }));

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void Numeric_Raises_ValueChangedEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var raised = false;
            var value = 3.5;
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; }));

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void Numeric_Raises_ChangedAndValueChangedEventOnStepUp()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var raised = false;
            var value = 3.5;
            var expectedValue = 5.5;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<double>(p => p.Value, value);
                parameters.Add(p => p.Step, "2");
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-numeric-up").Click();

            Assert.True(raised, "Numeric Change should be raised on step up");
            Assert.True(object.Equals(expectedValue, newValue), $"Numeric value should be incremented on step up. Expected value: {expectedValue}, value: {newValue}");

            raised = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; }));

            component.Find(".rz-numeric-up").Click();

            Assert.True(raised, "Numeric ValueChanged should be raised on step up");
        }

        [Fact]
        public void Numeric_Raises_ChangedAndValueChangedEventOnStepDown()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            var raised = false;
            var value = 3.5;
            var expectedValue = 1.5;
            object newValue = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<double>(p => p.Value, value);
                parameters.Add(p => p.Step, "2");
                parameters.Add(p => p.Change, args => { raised = true; newValue = args; });
            });

            component.Find(".rz-numeric-down").Click();

            Assert.True(raised, "Numeric Change should be raised on step up");
            Assert.True(object.Equals(expectedValue, newValue), $"Numeric value should be incremented on step up. Expected value: {expectedValue}, value: {newValue}");

            raised = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; }));

            component.Find(".rz-numeric-down").Click();

            Assert.True(raised, "Numeric ValueChanged should be raised on step up");
        }

        [Fact]
        public void Numeric_UpDown_Rendered()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            component.Render();

            Assert.Contains(@$"rz-numeric-button-icon", component.Markup);
            Assert.Contains(@$"rz-numeric-up", component.Markup);
            Assert.Contains(@$"rz-numeric-down", component.Markup);
        }

        [Fact]
        public void Numeric_UpDown_NotRenderedIfHidden()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>(ComponentParameter.CreateParameter(nameof(RadzenNumeric<double>.ShowUpDown), false));

            component.Render();

            Assert.DoesNotContain(@$"rz-numeric-button-icon", component.Markup);
            Assert.DoesNotContain(@$"rz-numeric-up", component.Markup);
            Assert.DoesNotContain(@$"rz-numeric-down", component.Markup);
        }

        [Fact]
        public void Numeric_Formatted()
        {
            using var ctx = new TestContext();

            double valueToTest = 100.234;
            string format = "0.00";

            var component = ctx.RenderComponent<RadzenNumeric<double>>(
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<double>.Format), format),
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<double>.Value), valueToTest)
            );

            component.Render();

            Assert.Contains($" value=\"{valueToTest.ToString(format)}\"", component.Markup);
        }
        
        public static TheoryData<decimal, decimal> NumericFormatterPreservesLeadingZerosData =>
            new()
            {
                { 10.000m, 100.000m },
                { 100.000m, 10.000m }
            };
        
        [Theory]
        [MemberData(nameof(NumericFormatterPreservesLeadingZerosData))]
        public void Numeric_Formatter_PreservesLeadingZeros(decimal oldValue, decimal newValue)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            string format = "0.000";

            var component = ctx.RenderComponent<RadzenNumeric<decimal>>(
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<decimal>.Format), format),
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<decimal>.Value), oldValue)
            );

            component.Render();
            
            Assert.Contains($" value=\"{oldValue.ToString(format)}\"", component.Markup);

            component.Find("input").Change(newValue);
            
            Assert.Contains($" value=\"{newValue.ToString(format)}\"", component.Markup);
        }

        [Fact]
        public void Numeric_Uses_ConvertValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var value = new Dollars(11m);
            Dollars? ConvertFunc(string s) => decimal.TryParse(s, out var val) ? new Dollars(val) : null;
            var component = ctx.RenderComponent<RadzenNumeric<Dollars?>>(
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<Dollars?>.ConvertValue), (Func<string, Dollars?>)ConvertFunc),
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<Dollars?>.Value), value)
            );

            component.Render();
            
            Assert.Contains($" value=\"{value.ToString()}\"", component.Markup);

            var newValue = new Dollars(13.53m);
            component.Find("input").Change("13.53");

            Assert.Contains($" value=\"{newValue.ToString()}\"", component.Markup);
        }

        [Fact]
        public void Numeric_Supports_TypeConverter()
        {
            using var ctx = new TestContext();

            var valueToTest = new Dollars(100.234m);
            string format = "0.00";

            var component = ctx.RenderComponent<RadzenNumeric<Dollars>>(
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<Dollars>.Format), format),
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<Dollars>.Value), valueToTest)
            );

            component.Render();

            Assert.Contains($" value=\"{valueToTest.ToString(format)}\"", component.Markup);
        }

        [Fact]
        public void Numeric_Supports_IComparable()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenNumeric<Dollars>>();

            var maxValue = 2;

            component.SetParametersAndRender(parameters =>
            {
                component.SetParametersAndRender(parameters =>
                {
                    parameters.Add(p => p.Value, new Dollars(1m));
                    parameters.Add(p => p.Max, maxValue);
                });
            });
            
            component.Find("input").Change("13.53");

            var maxDollars = new Dollars(2);
            Assert.Contains($" value=\"{maxDollars.ToString()}\"", component.Markup);
            Assert.Equal(component.Instance.Value, maxDollars);
        }

        [Fact]
        public void Numeric_Supports_IFormattable()
        {
            using var ctx = new TestContext();

            var valueToTest = new Temperature(60.23m);
            const string format = "F";

            var component = ctx.RenderComponent<RadzenNumeric<Temperature>>(
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<Temperature>.Format), format),
                ComponentParameter.CreateParameter(nameof(RadzenNumeric<Temperature>.Value), valueToTest)
            );

            component.Render();

            var input = component.Find("input").GetAttribute("value");
            input.MarkupMatches(valueToTest.ToString(format));
        }
    }
}
