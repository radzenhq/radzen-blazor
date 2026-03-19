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

        [Fact]
        public void ToggleButton_Renders_Variant()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Outlined);
            });

            Assert.Contains("rz-variant-outlined", component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_ToggleButtonStyle_WhenActive()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Value, true);
                parameters.Add(p => p.ToggleButtonStyle, ButtonStyle.Info);
            });

            Assert.Contains("rz-info", component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_ToggleShade_WhenActive()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Value, true);
                parameters.Add(p => p.ToggleShade, Shade.Lighter);
            });

            Assert.Contains("rz-shade-lighter", component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_ToggleIcon_WhenActive()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Icon, "toggle_off");
                parameters.Add(p => p.ToggleIcon, "toggle_on");
                parameters.Add(p => p.Value, true);
            });

            Assert.Contains("toggle_on", component.Markup);
        }

        [Fact]
        public void ToggleButton_Renders_NormalIcon_WhenInactive()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Icon, "toggle_off");
                parameters.Add(p => p.ToggleIcon, "toggle_on");
                parameters.Add(p => p.Value, false);
            });

            Assert.Contains("toggle_off", component.Markup);
            Assert.DoesNotContain("toggle_on", component.Markup);
        }

        [Fact]
        public void ToggleButton_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-button", component.Markup);
        }

        [Fact]
        public void ToggleButton_Click_Toggles_Value()
        {
            using var ctx = new TestContext();
            bool newValue = false;

            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Value, false);
                parameters.Add(p => p.ValueChanged, (bool v) => newValue = v);
            });

            component.Find("button").Click();

            Assert.True(newValue);
        }

        [Fact]
        public void ToggleButton_Disabled_Click_DoesNotToggle()
        {
            using var ctx = new TestContext();
            var changed = false;

            var component = ctx.RenderComponent<RadzenToggleButton>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Change, (bool _) => changed = true);
            });

            component.Find("button").Click();

            Assert.False(changed);
        }

        [Fact]
        public void ToggleButton_HasClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenToggleButton>();

            Assert.Contains("rz-toggle-button", component.Markup);
        }
    }
}

