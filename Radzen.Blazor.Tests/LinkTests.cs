using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LinkTests
    {
        [Fact]
        public void Link_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Link_Renders_TextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            var text = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            var textElement = component.Find(".rz-link-text");

            Assert.NotNull(textElement);
            Assert.Equal(text, textElement.TextContent.Trim());
        }

        [Fact]
        public void Link_Renders_IconParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            var icon = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(@$"<i class=""notranslate rzi"" aria-hidden=""true"">{icon}</i>", component.Markup);
        }

        [Fact]
        public void Link_Renders_PathParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            var path = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Path, path));

            Assert.Contains(@$"<a href=""{path}""", component.Markup);
        }

        [Fact]
        public void Link_Renders_TargetParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            var target = "_blank";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Target, target));

            Assert.Contains(@$"target=""{target}""", component.Markup);
        }

        [Fact]
        public void Link_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Contains("class=\"rz-link rz-link-disabled active\"", component.Markup);

            Assert.DoesNotContain("href=", component.Markup);
        }

        [Fact]
        public void Link_Disabled_SetsAriaDisabled()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Path, "/home");
                parameters.Add(p => p.Disabled, true);
            });

            var anchor = component.Find("a");

            Assert.Equal("true", anchor.GetAttribute("aria-disabled"));
            Assert.Equal("-1", anchor.GetAttribute("tabindex"));
            Assert.False(anchor.HasAttribute("href"));
        }

        [Fact]
        public void Link_Enabled_DoesNotSetAriaDisabled()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Path, "/home"));

            var anchor = component.Find("a");

            Assert.False(anchor.HasAttribute("aria-disabled"));
            Assert.False(anchor.HasAttribute("tabindex"));
            Assert.True(anchor.HasAttribute("href"));
        }

        [Fact]
        public void Link_Icon_IsAriaHidden()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, "home"));

            var icon = component.Find("i.rzi");

            Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        }

        [Fact]
        public void Link_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLink>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("data-testid", "my-link"));

            Assert.Contains(@"data-testid=""my-link""", component.Markup);
        }

        [Fact]
        public void Link_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLink>();

            Assert.Contains("rz-link", component.Markup);
        }

        [Fact]
        public void Link_Renders_ATag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLink>(parameters =>
            {
                parameters.Add(p => p.Path, "/home");
            });

            Assert.Contains("<a", component.Markup);
        }

        [Fact]
        public void Link_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLink>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-link", component.Markup);
        }

        [Fact]
        public void Link_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLink>(parameters =>
            {
                parameters.Add(p => p.Path, "/test");
                parameters.AddChildContent("<span>Custom Link</span>");
            });

            Assert.Contains("Custom Link", component.Markup);
        }
    }
}
