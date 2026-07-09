using Bunit;
using System.Globalization;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class VirtualKeyboardTests
    {
        [Fact]
        public void VirtualKeyboard_Renders_WithClassNameAndChildContent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.AddChildContent("<input class=\"my-input\" />"));

            Assert.Contains(@"rz-virtual-keyboard", component.Markup);
            Assert.Contains(@"my-input", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.Add(p => p.Visible, false));

            Assert.Empty(component.Markup.Trim());
        }

        [Fact]
        public void VirtualKeyboard_Renders_AlphanumericLayoutByDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>();

            Assert.Contains(@"data-vk-key=""q""", component.Markup);
            Assert.Contains(@"data-vk-shift=""Q""", component.Markup);
            Assert.Contains(@"rz-virtual-keyboard-cap-shift"">Q</span>", component.Markup);
            Assert.Contains(@"data-vk-action=""shift""", component.Markup);
            Assert.Contains(@"data-vk-action=""backspace""", component.Markup);
            Assert.Contains(@"data-vk-action=""enter""", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_SingleSectionByDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>();

            Assert.Single(component.FindAll(".rz-virtual-keyboard-section"));
        }

        [Fact]
        public void VirtualKeyboard_Renders_NumpadType()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.Add(p => p.Type, VirtualKeyboardType.Numpad));

            Assert.Contains(@"data-vk-key=""7""", component.Markup);
            Assert.DoesNotContain(@"data-vk-key=""q""", component.Markup);
            Assert.Single(component.FindAll(".rz-virtual-keyboard-section"));
        }

        [Fact]
        public void VirtualKeyboard_Renders_AllType_WithTwoSections()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.Add(p => p.Type, VirtualKeyboardType.All));

            Assert.Contains(@"data-vk-key=""q""", component.Markup);
            Assert.Equal(2, component.FindAll(".rz-virtual-keyboard-section").Count);
        }

        [Fact]
        public void VirtualKeyboard_Renders_CultureDecimalSeparator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
            {
                parameters.Add(p => p.Type, VirtualKeyboardType.Numpad);
                parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("de-DE"));
            });

            Assert.Contains(@"data-vk-key="",""", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_DecimalSeparatorOverride()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
            {
                parameters.Add(p => p.Type, VirtualKeyboardType.Numpad);
                parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("de-DE"));
                parameters.Add(p => p.DecimalSeparator, ".");
            });

            Assert.Contains(@"data-vk-key="".""", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_CustomLayout()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.Add(p => p.Layout, VirtualKeyboardLayout.FromRows("7 8 9 °C", "{clear} {backspace} {enter}")));

            Assert.Contains(@"data-vk-key=""°C""", component.Markup);
            Assert.Contains(@"data-vk-action=""clear""", component.Markup);
            Assert.DoesNotContain(@"data-vk-key=""q""", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_ShiftLayoutFromPreset()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.Add(p => p.Layout, VirtualKeyboardLayout.Qwertz));

            Assert.Contains(@"data-vk-key=""z""", component.Markup);
            Assert.Contains(@"data-vk-key=""ü""", component.Markup);
            Assert.Contains(@"data-vk-shift=""Ü""", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_BottomPlacementByDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>();

            Assert.Contains(@"rz-virtual-keyboard-bottom", component.Markup);
            Assert.Contains(@"display:none", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_TopPlacement()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.Add(p => p.Placement, VirtualKeyboardPlacement.Top));

            Assert.Contains(@"rz-virtual-keyboard-top", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_InlinePlacement_Visible()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>(parameters =>
                parameters.Add(p => p.Placement, VirtualKeyboardPlacement.Inline));

            Assert.Contains(@"rz-virtual-keyboard-inline", component.Markup);
            Assert.DoesNotContain(@"display:none", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_AriaLabels()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>();

            Assert.Contains(@"aria-label=""Virtual keyboard""", component.Markup);
            Assert.Contains(@"aria-label=""Backspace""", component.Markup);
        }

        [Fact]
        public void VirtualKeyboard_Renders_ShiftKeyAsToggleButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>();

            var shiftKey = component.Find(@"[data-vk-action=""shift""]");

            Assert.Equal("false", shiftKey.GetAttribute("aria-pressed"));
        }

        [Fact]
        public void VirtualKeyboard_Renders_KeysNotFocusable()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenVirtualKeyboard>();

            foreach (var key in component.FindAll(".rz-virtual-keyboard-key"))
            {
                Assert.Equal("-1", key.GetAttribute("tabindex"));
                Assert.Equal("button", key.GetAttribute("type"));
            }
        }

        [Fact]
        public void VirtualKeyboard_CreatesJSInstance()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            ctx.RenderComponent<RadzenVirtualKeyboard>();

            Assert.Contains(ctx.JSInterop.Invocations, invocation => invocation.Identifier == "Radzen.createVirtualKeyboard");
        }
    }
}
