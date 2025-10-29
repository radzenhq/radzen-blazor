using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class CardTests
    {
        [Fact]
        public void Card_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCard>();

            Assert.Contains(@"rz-card", component.Markup);
        }

        [Fact]
        public void Card_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCard>(parameters =>
            {
                parameters.AddChildContent("<div>Card Content</div>");
            });

            Assert.Contains("Card Content", component.Markup);
        }

        [Fact]
        public void Card_Renders_Variant()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCard>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Variant, Variant.Outlined));
            Assert.Contains("rz-variant-outlined", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Variant, Variant.Filled));
            Assert.Contains("rz-variant-filled", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Variant, Variant.Flat));
            Assert.Contains("rz-variant-flat", component.Markup);
        }
    }
}

