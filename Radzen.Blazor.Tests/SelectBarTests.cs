using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SelectBarTests
    {
        [Fact]
        public void SelectBar_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>();

            Assert.Contains(@"rz-selectbar", component.Markup);
        }

        [Fact]
        public void SelectBar_Renders_Orientation()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Horizontal));
            Assert.Contains("rz-selectbar-horizontal", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Vertical));
            Assert.Contains("rz-selectbar-vertical", component.Markup);
        }

        [Fact]
        public void SelectBar_Renders_Multiple()
        {
            using var ctx = new TestContext();
            // When Multiple is true, TValue should be IEnumerable<T>
            var component = ctx.RenderComponent<RadzenSelectBar<System.Collections.Generic.IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            Assert.NotNull(component.Instance);
            Assert.True(component.Instance.Multiple);
        }

        [Fact]
        public void SelectBar_Renders_Size()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, ButtonSize.Small));
            Assert.Contains("rz-button-sm", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, ButtonSize.Large));
            Assert.Contains("rz-button-lg", component.Markup);
        }
    }
}

