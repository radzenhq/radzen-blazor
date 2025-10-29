using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ToggleButtonTests
    {
        [Fact]
        public void ToggleButton_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            Assert.Contains(@"rz-button", component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            var text = "Toggle Me";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains(text, component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_IconParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            var icon = "toggle_on";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(icon, component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_Value()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, true));

            Assert.Contains("rz-state-active", component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_Disabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_ButtonStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ButtonStyle, ButtonStyle.Primary));
            Assert.Contains("rz-primary", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ButtonStyle, ButtonStyle.Success));
            Assert.Contains("rz-success", component.Markup);
        }

        [Fact]
        public void ToggleButton_Raises_ChangeEvent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            var changed = false;
            bool newValue = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args =>
            {
                changed = true;
                newValue = args;
            }));

            component.Find("button").Click();

            Assert.True(changed);
            Assert.True(newValue);
        }
    }
}

