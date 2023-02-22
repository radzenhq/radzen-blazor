using Bunit;
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

            Assert.Contains(@$"rz-spinner", component.Markup);
            Assert.Contains(@$"rz-spinner-up", component.Markup);
            Assert.Contains(@$"rz-spinner-down", component.Markup);
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

            component.Find(".rz-spinner-down").Click();

            Assert.False(raised, $"Numeric value should Change event if value is less than min value.");
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

            component.Find(".rz-spinner-up").Click();

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

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AutoComplete, true));

            Assert.Contains(@$"autocomplete=""on""", component.Markup);
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

            component.Find(".rz-spinner-up").Click();

            Assert.True(raised, "Numeric Change should be raised on step up");
            Assert.True(object.Equals(expectedValue, newValue), $"Numeric value should be incremented on step up. Expected value: {expectedValue}, value: {newValue}");

            raised = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; }));

            component.Find(".rz-spinner-up").Click();

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

            component.Find(".rz-spinner-down").Click();

            Assert.True(raised, "Numeric Change should be raised on step up");
            Assert.True(object.Equals(expectedValue, newValue), $"Numeric value should be incremented on step up. Expected value: {expectedValue}, value: {newValue}");

            raised = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; }));

            component.Find(".rz-spinner-down").Click();

            Assert.True(raised, "Numeric ValueChanged should be raised on step up");
        }

        [Fact]
        public void Numeric_UpDown_Rendered()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>();

            component.Render();

            Assert.Contains(@$"rz-spinner-button-icon", component.Markup);
            Assert.Contains(@$"rz-spinner-up", component.Markup);
            Assert.Contains(@$"rz-spinner-down", component.Markup);
        }

        [Fact]
        public void Numeric_UpDown_NotRenderedIfHidden()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenNumeric<double>>(ComponentParameter.CreateParameter(nameof(RadzenNumeric<double>.ShowUpDown), false));

            component.Render();

            Assert.DoesNotContain(@$"rz-spinner-button-icon", component.Markup);
            Assert.DoesNotContain(@$"rz-spinner-up", component.Markup);
            Assert.DoesNotContain(@$"rz-spinner-down", component.Markup);
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
    }
}
