using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class FabTests
    {
        [Fact]
        public void Fab_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>();

            Assert.Contains("rz-fab", component.Markup);
            Assert.Contains("rz-button", component.Markup);
        }

        [Fact]
        public void Fab_Renders_IconParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.Icon, "add");
            });

            Assert.Contains("add", component.Markup);
        }

        [Fact]
        public void Fab_DefaultSize_IsLarge()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>();

            Assert.Equal(ButtonSize.Large, component.Instance.Size);
        }

        [Fact]
        public void Fab_Renders_IconOnly_Class()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.Icon, "add");
            });

            Assert.Contains("rz-button-icon-only", component.Markup);
        }

        [Fact]
        public void Fab_Renders_ButtonStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.ButtonStyle, ButtonStyle.Primary);
            });

            Assert.Contains("rz-primary", component.Markup);
        }

        [Fact]
        public void Fab_Renders_Variant()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Outlined);
            });

            Assert.Contains("rz-variant-outlined", component.Markup);
        }

        [Fact]
        public void Fab_Renders_DisabledState()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Fab_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.Text, "Add Item");
            });

            Assert.Contains("Add Item", component.Markup);
        }

        [Fact]
        public void Fab_Renders_Shade()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.Shade, Shade.Lighter);
            });

            Assert.Contains("rz-shade-lighter", component.Markup);
        }

        [Fact]
        public void Fab_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFab>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-fab", component.Markup);
        }
    }
}
