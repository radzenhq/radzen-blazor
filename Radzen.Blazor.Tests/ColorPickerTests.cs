using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ColorPickerTests
    {
        [Fact]
        public void ColorPicker_ShouldAcceptInvalidValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(ComponentParameter.CreateParameter("Value", "invalid"));
        }

        [Fact]
        public void ColorPicker_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>();

            Assert.Contains("rz-colorpicker", component.Markup);
        }

        [Fact]
        public void ColorPicker_Renders_WithValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(parameters =>
            {
                parameters.Add(p => p.Value, "#FF0000");
            });

            Assert.Contains("rz-colorpicker", component.Markup);
        }

        [Fact]
        public void ColorPicker_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void ColorPicker_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-colorpicker", component.Markup);
        }

        [Fact]
        public void ColorPicker_Renders_Icon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(parameters =>
            {
                parameters.Add(p => p.Icon, "palette");
            });

            Assert.Contains("palette", component.Markup);
        }

        [Fact]
        public void ColorPicker_Renders_ToggleAriaLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(parameters =>
            {
                parameters.Add(p => p.ToggleAriaLabel, "Pick color");
            });

            Assert.Contains("Pick color", component.Markup);
        }

        [Fact]
        public void ColorPicker_AcceptsNullValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(parameters =>
            {
                parameters.Add(p => p.Value, (string)null!);
            });

            Assert.Contains("rz-colorpicker", component.Markup);
        }

        [Fact]
        public void ColorPicker_AcceptsHexValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenColorPicker>(parameters =>
            {
                parameters.Add(p => p.Value, "#00FF00");
            });

            Assert.Contains("rz-colorpicker", component.Markup);
        }
    }
}
