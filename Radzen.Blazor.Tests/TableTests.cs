using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TableTests
    {
        [Fact]
        public void Table_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>();

            Assert.Contains(@"rz-datatable", component.Markup);
        }

        [Fact]
        public void Table_Renders_TableElement()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>();

            Assert.Contains("rz-grid-table", component.Markup);
            Assert.Contains("<table", component.Markup);
        }

        [Fact]
        public void Table_Renders_GridLines_None()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>(parameters =>
            {
                parameters.Add(p => p.GridLines, DataGridGridLines.None);
            });

            Assert.Contains("rz-grid-gridlines-none", component.Markup);
        }

        [Fact]
        public void Table_Renders_GridLines_Horizontal()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>(parameters =>
            {
                parameters.Add(p => p.GridLines, DataGridGridLines.Horizontal);
            });

            Assert.Contains("rz-grid-gridlines-horizontal", component.Markup);
        }

        [Fact]
        public void Table_Renders_GridLines_Vertical()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>(parameters =>
            {
                parameters.Add(p => p.GridLines, DataGridGridLines.Vertical);
            });

            Assert.Contains("rz-grid-gridlines-vertical", component.Markup);
        }

        [Fact]
        public void Table_Renders_GridLines_Both()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>(parameters =>
            {
                parameters.Add(p => p.GridLines, DataGridGridLines.Both);
            });

            Assert.Contains("rz-grid-gridlines-both", component.Markup);
        }

        [Fact]
        public void Table_Renders_AllowAlternatingRows_True()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>(parameters =>
            {
                parameters.Add(p => p.AllowAlternatingRows, true);
            });

            Assert.Contains("rz-grid-table-striped", component.Markup);
        }

        [Fact]
        public void Table_Renders_AllowAlternatingRows_False()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTable>(parameters =>
            {
                parameters.Add(p => p.AllowAlternatingRows, false);
            });

            Assert.DoesNotContain("rz-grid-table-striped", component.Markup);
        }
    }
}

