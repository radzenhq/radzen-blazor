using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class HeadingTests
    {
        [Fact]
        public void Heading_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            Assert.Contains(@"rz-heading", component.Markup);
        }

        [Fact]
        public void Heading_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            var text = "Test Heading";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains(text, component.Markup);
        }

        [Fact]
        public void Heading_Renders_H1_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            var text = "Heading Text";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains("<h1", component.Markup);
        }

        [Fact]
        public void Heading_Renders_H2_Size()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            var text = "Heading 2";
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Text, text);
                parameters.Add(p => p.Size, "H2");
            });

            Assert.Contains("<h2", component.Markup);
            Assert.Contains(text, component.Markup);
        }

        [Fact]
        public void Heading_Renders_H3_Size()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, "H3"));

            Assert.Contains("<h3", component.Markup);
        }

        [Fact]
        public void Heading_Renders_H4_Size()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, "H4"));

            Assert.Contains("<h4", component.Markup);
        }

        [Fact]
        public void Heading_Renders_H5_Size()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, "H5"));

            Assert.Contains("<h5", component.Markup);
        }

        [Fact]
        public void Heading_Renders_H6_Size()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeading>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, "H6"));

            Assert.Contains("<h6", component.Markup);
        }
    }
}

