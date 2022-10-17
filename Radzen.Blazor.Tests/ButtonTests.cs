using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ButtonTests
    {
        [Fact]
        public void Button_Renders_TextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            var text = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void Button_Renders_IconParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            var icon = "account_circle";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(@$"<i class=""rz-button-icon-left rzi"">{icon}</i>", component.Markup);
        }

        [Fact]
        public void Button_Renders_BusySpinner()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            var icon = "account_circle";

            component.SetParametersAndRender(parameters => parameters
                .Add(p => p.Icon, icon)
                .Add(p => p.IsBusy, true)
            );

            // does not render the actual icon when busy
            Assert.DoesNotContain(@$"<i class=""rz-button-icon-left rzi"">{icon}</i>", component.Markup);

            // renders the icon with busy spin animation
            Assert.Contains(@"<i style=""animation: rotation", component.Markup);
            Assert.Contains(">refresh</i>", component.Markup);
        }

        [Fact]
        public void Button_Renders_IconAndTextParameters()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            var text = "Test";
            var icon = "account_circle";

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Text, text);
                parameters.Add(p => p.Icon, icon);
            });

            Assert.Contains(@$"<i class=""rz-button-icon-left rzi"">{icon}</i>", component.Markup);
            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void Button_Renders_ImageParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            var image = "test.png";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Image, image));

            Assert.Contains(@$"<img class=""rz-button-icon-left rzi"" src=""{image}"" />", component.Markup);
        }

        [Fact]
        public void Button_Renders_ImageAndTextParameters()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            var text = "Test";
            var image = "test.png";

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Text, text);
                parameters.Add(p => p.Image, image);
            });

            Assert.Contains(@$"<img class=""rz-button-icon-left rzi"" src=""{image}"" />", component.Markup);
            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void Button_Renders_SizeParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, ButtonSize.Medium));

            Assert.Contains(@$"rz-button-md", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, ButtonSize.Small));

            Assert.Contains(@$"rz-button-sm", component.Markup);
        }

        [Fact]
        public void Button_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ButtonStyle, ButtonStyle.Primary));

            Assert.Contains(@$"rz-primary", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ButtonStyle, ButtonStyle.Secondary));

            Assert.Contains(@$"rz-secondary", component.Markup);
        }

        [Fact]
        public void Button_Renders_TypeParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ButtonType, ButtonType.Button));

            Assert.Contains(@$"type=""button""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ButtonType, ButtonType.Submit));

            Assert.Contains(@$"type=""submit""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ButtonType, ButtonType.Reset));

            Assert.Contains(@$"type=""reset""", component.Markup);
        }

        [Fact]
        public void Button_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Button_Renders_ChildContent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            component.SetParametersAndRender(parameters => parameters.AddChildContent("SomeContent"));

            Assert.Contains(@$"SomeContent", component.Markup);
        }

        [Fact]
        public void Button_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Button_Raises_ClickEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenButton>();

            var clicked = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Click, args => { clicked = true; }));

            component.Find("button").Click();

            Assert.True(clicked);
        }
    }
}
