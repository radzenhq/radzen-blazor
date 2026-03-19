using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TextTests
    {
        [Fact]
        public void Text_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.Text, "Hello World");
            });

            Assert.Contains("Hello World", component.Markup);
        }

        [Fact]
        public void Text_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.AddChildContent("Custom Content");
            });

            Assert.Contains("Custom Content", component.Markup);
        }

        [Fact]
        public void Text_TextParameter_TakesPrecedence_OverChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.Text, "Text Wins");
                parameters.AddChildContent("Child Loses");
            });

            Assert.Contains("Text Wins", component.Markup);
            Assert.DoesNotContain("Child Loses", component.Markup);
        }

        [Fact]
        public void Text_DefaultStyle_IsBody1()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>();

            Assert.Contains("rz-text-body1", component.Markup);
            Assert.Contains("<p", component.Markup);
        }

        [Fact]
        public void Text_H1_RendersH1Tag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.H1);
                parameters.Add(p => p.Text, "Heading");
            });

            Assert.Contains("<h1", component.Markup);
            Assert.Contains("rz-text-h1", component.Markup);
        }

        [Fact]
        public void Text_H2_RendersH2Tag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.H2);
            });

            Assert.Contains("<h2", component.Markup);
            Assert.Contains("rz-text-h2", component.Markup);
        }

        [Fact]
        public void Text_H3_RendersH3Tag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.H3);
            });

            Assert.Contains("<h3", component.Markup);
            Assert.Contains("rz-text-h3", component.Markup);
        }

        [Fact]
        public void Text_DisplayH1_RendersH1Tag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.DisplayH1);
            });

            Assert.Contains("<h1", component.Markup);
            Assert.Contains("rz-text-display-h1", component.Markup);
        }

        [Fact]
        public void Text_Subtitle1_RendersH6Tag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.Subtitle1);
            });

            Assert.Contains("<h6", component.Markup);
            Assert.Contains("rz-text-subtitle1", component.Markup);
        }

        [Fact]
        public void Text_Body2_RendersPTag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.Body2);
            });

            Assert.Contains("<p", component.Markup);
            Assert.Contains("rz-text-body2", component.Markup);
        }

        [Fact]
        public void Text_Caption_RendersSpanTag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.Caption);
            });

            Assert.Contains("<span", component.Markup);
            Assert.Contains("rz-text-caption", component.Markup);
        }

        [Fact]
        public void Text_Overline_RendersSpanTag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.Overline);
            });

            Assert.Contains("<span", component.Markup);
            Assert.Contains("rz-text-overline", component.Markup);
        }

        [Fact]
        public void Text_ButtonStyle_RendersSpanTag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.Button);
            });

            Assert.Contains("<span", component.Markup);
            Assert.Contains("rz-text-button", component.Markup);
        }

        [Fact]
        public void Text_TagName_OverridesAutoTag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.H1);
                parameters.Add(p => p.TagName, TagName.Div);
            });

            // TagName.Div should override the auto h1 tag
            Assert.Contains("<div", component.Markup);
            Assert.Contains("rz-text-h1", component.Markup);
        }

        [Fact]
        public void Text_TagName_Pre()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TagName, TagName.Pre);
                parameters.Add(p => p.Text, "code here");
            });

            Assert.Contains("<pre", component.Markup);
        }

        [Fact]
        public void Text_TagName_A()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TagName, TagName.A);
            });

            Assert.Contains("<a", component.Markup);
        }

        [Fact]
        public void Text_TagName_Button()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TagName, TagName.Button);
            });

            Assert.Contains("<button", component.Markup);
        }

        [Fact]
        public void Text_TextAlign_Center()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextAlign, TextAlign.Center);
            });

            Assert.Contains("rz-text-align-center", component.Markup);
        }

        [Fact]
        public void Text_TextAlign_Right()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextAlign, TextAlign.Right);
            });

            Assert.Contains("rz-text-align-right", component.Markup);
        }

        [Fact]
        public void Text_TextAlign_Justify()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextAlign, TextAlign.Justify);
            });

            Assert.Contains("rz-text-align-justify", component.Markup);
        }

        [Fact]
        public void Text_TextAlign_Left_DoesNotAddAlignClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextAlign, TextAlign.Left);
            });

            Assert.DoesNotContain("rz-text-align-", component.Markup);
        }

        [Fact]
        public void Text_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
                parameters.Add(p => p.Text, "Hidden");
            });

            Assert.DoesNotContain("Hidden", component.Markup);
        }

        [Fact]
        public void Text_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.Style, "color:red");
            });

            Assert.Contains("color:red", component.Markup);
        }

        [Fact]
        public void Text_Renders_Anchor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenText>(parameters =>
            {
                parameters.Add(p => p.TextStyle, TextStyle.H2);
                parameters.Add(p => p.Text, "Section Title");
                parameters.Add(p => p.Anchor, "section-title");
            });

            Assert.Contains("section-title", component.Markup);
            Assert.Contains("rz-link", component.Markup);
        }
    }
}
