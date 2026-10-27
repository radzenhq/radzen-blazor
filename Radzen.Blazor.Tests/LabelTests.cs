using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LabelTests
    {
        [Fact]
        public void Label_Renders_TextParameter()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenLabel>();

            var value = "Test";

            component.Render(parameters => parameters.Add(p => p.Text, value));

            Assert.Contains(@$">{value}</label>", component.Markup);
        }

        [Fact]
        public void Label_Renders_ComponentParameter()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenLabel>();

            var value = "test";

            component.Render(parameters => parameters.Add(p => p.Component, value));

            Assert.Contains(@$"for=""{value}""", component.Markup);
        }

        [Fact]
        public void Label_Renders_StyleParameter()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenLabel>();

            var value = "width:20px";

            component.Render(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Label_Renders_UnmatchedParameter()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenLabel>();

            component.Render(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Label_Renders_WithClassName()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenLabel>();

            Assert.Contains("rz-label", component.Markup);
        }

        [Fact]
        public void Label_Renders_LabelTag()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenLabel>();

            Assert.Contains("<label", component.Markup);
        }

        [Fact]
        public void Label_Renders_ChildContent()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenLabel>(parameters =>
            {
                parameters.AddChildContent("<strong>Required</strong>");
            });

            Assert.Contains("<strong>Required</strong>", component.Markup);
        }

        [Fact]
        public void Label_NotVisible_DoesNotRender()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenLabel>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("<label", component.Markup);
        }
    }
}
