using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class RowTests
    {
        [Fact]
        public void Row_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>();

            Assert.Contains(@"rz-row", component.Markup);
        }

        [Fact]
        public void Row_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters =>
            {
                parameters.AddChildContent("<div>Row Content</div>");
            });

            Assert.Contains("Row Content", component.Markup);
        }

        [Fact]
        public void Row_Renders_Gap()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Gap, "2rem"));

            Assert.Contains("--rz-gap:2rem", component.Markup);
        }

        [Fact]
        public void Row_Renders_RowGap()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.RowGap, "1.5rem"));

            Assert.Contains("--rz-row-gap:1.5rem", component.Markup);
        }

        [Fact]
        public void Row_Renders_ColumnGap()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Gap, "1rem"));

            Assert.Contains("--rz-gap:1rem", component.Markup);
        }
    }
}

