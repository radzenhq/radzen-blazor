using System;
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

        [Fact]
        public void Row_DoesNotRender_RowColsClass_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>();

            Assert.DoesNotContain("rz-row-cols", component.Markup);
        }

        [Fact]
        public void Row_Renders_Columns()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters => parameters.Add(p => p.Columns, 4));

            Assert.Contains("rz-row-cols-4", component.Find(".rz-row").ClassList);
        }

        [Fact]
        public void Row_Renders_ResponsiveColumns()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters => parameters
                .Add(p => p.Columns, 1)
                .Add(p => p.ColumnsXS, 2)
                .Add(p => p.ColumnsSM, 3)
                .Add(p => p.ColumnsMD, 4)
                .Add(p => p.ColumnsLG, 5)
                .Add(p => p.ColumnsXL, 6)
                .Add(p => p.ColumnsXX, 7));

            var classList = component.Find(".rz-row").ClassList;

            Assert.Contains("rz-row-cols-1", classList);
            Assert.Contains("rz-row-cols-xs-2", classList);
            Assert.Contains("rz-row-cols-sm-3", classList);
            Assert.Contains("rz-row-cols-md-4", classList);
            Assert.Contains("rz-row-cols-lg-5", classList);
            Assert.Contains("rz-row-cols-xl-6", classList);
            Assert.Contains("rz-row-cols-xx-7", classList);
        }

        [Fact]
        public void Row_Columns_KeepsAlignmentClasses()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRow>(parameters => parameters
                .Add(p => p.Columns, 3)
                .Add(p => p.AlignItems, AlignItems.Center));

            var classList = component.Find(".rz-row").ClassList;

            Assert.Contains("rz-display-flex", classList);
            Assert.Contains("rz-align-items-center", classList);
            Assert.Contains("rz-row-cols-3", classList);
        }

        [Fact]
        public void Row_Throws_WhenColumnsOutOfRange()
        {
            using var ctx = new TestContext();

            Assert.Throws<ArgumentOutOfRangeException>(() => ctx.RenderComponent<RadzenRow>(parameters => parameters.Add(p => p.Columns, 13)));
        }
    }
}
