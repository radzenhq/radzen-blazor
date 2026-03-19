using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class BadgeTests
    {
        [Fact]
        public void Badge_Renders_TextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBadge>();

            var text = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains(text, component.Markup);
        }

        [Fact]
        public void Badge_Renders_ChildContent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBadge>();

            component.SetParametersAndRender(parameters => parameters.AddChildContent("SomeContent"));

            Assert.Contains(@$"SomeContent", component.Markup);
        }

        [Fact]
        public void Badge_Renders_BadgeStyle()
        {
            var badgeStyle = BadgeStyle.Danger;

            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBadge>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.BadgeStyle, badgeStyle));

            Assert.Contains($"badge-{badgeStyle.ToString().ToLower()}", component.Markup);
        }

        [Fact]
        public void Badge_Renders_Variant_Outlined()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Outlined);
            });

            Assert.Contains("rz-variant-outlined", component.Markup);
        }

        [Fact]
        public void Badge_Renders_Variant_Flat()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Flat);
            });

            Assert.Contains("rz-variant-flat", component.Markup);
        }

        [Fact]
        public void Badge_Renders_Variant_Text()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Text);
            });

            Assert.Contains("rz-variant-text", component.Markup);
        }

        [Fact]
        public void Badge_Renders_Shade()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.Shade, Shade.Lighter);
            });

            Assert.Contains("rz-shade-lighter", component.Markup);
        }

        [Fact]
        public void Badge_Renders_IsPill()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.IsPill, true);
            });

            Assert.Contains("rz-badge-pill", component.Markup);
        }

        [Fact]
        public void Badge_DoesNotRender_IsPill_WhenFalse()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.IsPill, false);
            });

            Assert.DoesNotContain("rz-badge-pill", component.Markup);
        }

        [Fact]
        public void Badge_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-badge", component.Markup);
        }

        [Fact]
        public void Badge_DefaultStyle_IsPrimary()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBadge>();

            Assert.Contains("rz-badge-primary", component.Markup);
        }

        [Fact]
        public void Badge_Renders_AllBadgeStyles()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBadge>(parameters =>
            {
                parameters.Add(p => p.BadgeStyle, BadgeStyle.Success);
            });
            Assert.Contains("rz-badge-success", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.BadgeStyle, BadgeStyle.Warning));
            Assert.Contains("rz-badge-warning", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.BadgeStyle, BadgeStyle.Info));
            Assert.Contains("rz-badge-info", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.BadgeStyle, BadgeStyle.Light));
            Assert.Contains("rz-badge-light", component.Markup);
        }
    }
}
