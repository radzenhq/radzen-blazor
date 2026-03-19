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

        [Fact]
        public void Row_Gap_NumericOnly_AppendsPx()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters =>
            {
                parameters.Add(p => p.Gap, "16");
            });

            Assert.Contains("--rz-gap:16px", component.Markup);
        }

        [Fact]
        public void Row_RowGap_NumericOnly_AppendsPx()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters =>
            {
                parameters.Add(p => p.RowGap, "8");
            });

            Assert.Contains("--rz-row-gap:8px", component.Markup);
        }

        [Fact]
        public void Row_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-row", component.Markup);
        }

        [Fact]
        public void Row_Renders_AlignItems()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters =>
            {
                parameters.Add(p => p.AlignItems, AlignItems.Center);
            });

            Assert.Contains("rz-align-items-center", component.Markup);
        }

        [Fact]
        public void Row_Renders_JustifyContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters =>
            {
                parameters.Add(p => p.JustifyContent, JustifyContent.SpaceBetween);
            });

            Assert.Contains("rz-justify-content-space-between", component.Markup);
        }

        [Fact]
        public void Row_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters =>
            {
                parameters.Add(p => p.Style, "padding:1rem");
            });

            Assert.Contains("padding:1rem", component.Markup);
        }
    }
}

