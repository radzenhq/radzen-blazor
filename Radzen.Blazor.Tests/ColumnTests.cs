using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ColumnTests
    {
        [Fact]
        public void Column_Renders_WithClassName()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenColumn>();

            Assert.Contains(@"rz-col", component.Markup);
        }

        [Fact]
        public void Column_Renders_ChildContent()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenColumn>(parameters =>
            {
                parameters.AddChildContent("<div>Column Content</div>");
            });

            Assert.Contains("Column Content", component.Markup);
        }

        [Fact]
        public void Column_Renders_SizeParameter()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenColumn>();

            component.Render(parameters => parameters.Add(p => p.Size, 6));

            Assert.Contains("rz-col-6", component.Markup);
        }

        [Fact]
        public void Column_Renders_SizeMD()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenColumn>();

            component.Render(parameters => parameters.Add(p => p.SizeMD, 4));

            Assert.Contains("rz-col-md-4", component.Markup);
        }

        [Fact]
        public void Column_Renders_SizeSM()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenColumn>();

            component.Render(parameters => parameters.Add(p => p.SizeSM, 12));

            Assert.Contains("rz-col-sm-12", component.Markup);
        }

        [Fact]
        public void Column_Renders_Offset()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenColumn>();

            component.Render(parameters => parameters.Add(p => p.Offset, 2));

            Assert.Contains("rz-offset-2", component.Markup);
        }
    }
}

