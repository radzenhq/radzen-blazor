using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SplitButtonTests
    {
        [Fact]
        public void SplitButton_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_TextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var text = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_IconParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var icon = "account_circle";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(@$"<i class=""rz-button-icon-left rzi"">{icon}</i>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_IconAndTextParameters()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var text = "Test";
            var icon = "account_circle";

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Text, text);
                parameters.Add(p => p.Icon, icon); 
            });

            Assert.Contains(@$"<i class=""rz-button-icon-left rzi"">{icon}</i>", component.Markup);
            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_ImageParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var image = "test.png";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Image, image));

            Assert.Contains(@$"<img class=""rz-button-icon-left rzi"" src=""{image}"" />", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_ImageAndTextParameters()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var text = "Test";
            var image = "test.png";

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Text, text);
                parameters.Add(p => p.Image, image);
            });

            Assert.Contains(@$"<img class=""rz-button-icon-left rzi"" src=""{image}"" />", component.Markup);
            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSplitButton>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void SplitButton_Raises_ClickEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var clicked = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Click, args => { clicked = true; }));

            component.Find("button").Click();

            Assert.True(clicked);
        }
    }
}
