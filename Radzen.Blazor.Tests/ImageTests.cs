using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ImageTests
    {
        [Fact]
        public void Image_Renders_PathParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenImage>();

            var path = "Test.png";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Path, path));

            Assert.Contains(@$"<img src=""{path}""", component.Markup);
        }

        [Fact]
        public void Image_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenImage>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Image_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenImage>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Image_Raises_ClickEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenImage>();

            var clicked = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Click, args => { clicked = true; }));

            component.Find("img").Click();

            Assert.True(clicked);
        }
    }
}
