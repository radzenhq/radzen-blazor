using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SwitchTests
    {
        [Fact]
        public void Switch_Renders_CssClass()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            Assert.Contains(@$"rz-switch", component.Markup);
        }

        [Fact]
        public void Switch_Renders_ValueParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Value, value));

            Assert.Contains(@$"rz-switch-checked", component.Markup);
        }


        [Fact]
        public void Switch_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Switch_Renders_NameParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"name=""{value}""", component.Markup);
        }

        [Fact]
        public void Switch_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }


        [Fact]
        public void Switch_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"rz-disabled", component.Markup);
        }

        [Fact]
        public void Switch_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }
        
        [Fact]
        public void Switch_Raises_ChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            var raised = false;
            var value = false;
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; newValue = args; }));

            component.Find("div").Click();

            Assert.True(raised);
            Assert.True(object.Equals(value, !(bool)newValue));
        }

        [Fact]
        public void Switch_Raises_ValueChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSwitch>();

            var raised = false;
            var value = false;
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; }));

            component.Find("div").Click();

            Assert.True(raised);
            Assert.True(object.Equals(value, !(bool)newValue));
        }
    }
}
