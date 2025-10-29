using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class RatingTests
    {
        [Fact]
        public void Rating_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRating>();

            Assert.Contains(@"rz-rating", component.Markup);
        }

        [Fact]
        public void Rating_Renders_Stars()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRating>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Stars, 5));

            // Should render 5 star icons (rzi-star or rzi-star-o) + 1 clear button icon = 6 total
            var starCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, "rz-rating-icon").Count;
            Assert.Equal(6, starCount); // 5 stars + 1 clear button
        }

        [Fact]
        public void Rating_Renders_CustomStarCount()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRating>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Stars, 10));

            var starCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, "rz-rating-icon").Count;
            Assert.Equal(11, starCount); // 10 stars + 1 clear button
        }

        [Fact]
        public void Rating_Renders_Value()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRating>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Value, 3);
                parameters.Add(p => p.Stars, 5);
            });

            // Should have 3 filled stars (rzi-star) and 2 outline stars (rzi-star-o)
            var filledStars = System.Text.RegularExpressions.Regex.Matches(component.Markup, "rzi-star\"").Count;
            Assert.Equal(3, filledStars);
        }

        [Fact]
        public void Rating_Renders_ReadOnly()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRating>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ReadOnly, true));

            Assert.Contains("rz-state-readonly", component.Markup);
        }

        [Fact]
        public void Rating_Renders_Disabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRating>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Contains("rz-state-disabled", component.Markup);
        }
    }
}

