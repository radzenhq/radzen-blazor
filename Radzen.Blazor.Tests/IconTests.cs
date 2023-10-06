using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class IconTests
    {
        [Fact]
        public void Icon_Renders_IconParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenIcon>();

            var icon = "account_circle";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(@$">{icon}</i>", component.Markup);
            Assert.Contains(@$"class=""rzi""", component.Markup);
        }

        [Fact]
        public void Icon_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenIcon>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Icon_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenIcon>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Icon_Renders_IconStyleClass()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenIcon>();

            component.SetParametersAndRender(parameters => parameters.Add(icon => icon.IconStyle, IconStyle.Primary));

            Assert.Contains(@$"rzi-primary", component.Markup);
        }

        [Fact]
        public void Icon_Renders_IconColor()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenIcon>();

            component.SetParametersAndRender(parameters => parameters.Add(icon => icon.IconColor, Colors.Primary));

            Assert.Contains(@$"color:", component.Markup);
        }

        [Fact]
        public void Icon_NotRenders_IconStyleClass_WhenNull()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenIcon>();

            component.SetParametersAndRender(parameters => parameters.Add(icon => icon.IconStyle, null));

            Assert.DoesNotContain(@$"rzi-primary", component.Markup);
        }

        [Fact]
        public void Icon_NotRenders_IconColor_WhenNull()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenIcon>();

            component.SetParametersAndRender(parameters => parameters.Add(icon => icon.IconColor, null));

            Assert.DoesNotContain(@$"color:", component.Markup);
        }
    }
}
