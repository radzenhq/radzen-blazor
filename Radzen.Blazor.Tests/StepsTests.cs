using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class StepsTests
    {
        [Fact]
        public void Steps_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>();

            Assert.Contains(@"rz-steps", component.Markup);
        }

        [Fact]
        public void Steps_Renders_ShowStepsButtons_True()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.ShowStepsButtons, true);
            });

            Assert.Contains("rz-steps-buttons", component.Markup);
        }

        [Fact]
        public void Steps_Renders_ShowStepsButtons_False()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.ShowStepsButtons, false);
            });

            Assert.DoesNotContain("rz-steps-buttons", component.Markup);
        }

        [Fact]
        public void Steps_Renders_StepsButtons()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.ShowStepsButtons, true);
            });

            Assert.Contains("rz-steps-prev", component.Markup);
            Assert.Contains("rz-steps-next", component.Markup);
        }

        [Fact]
        public void Steps_Renders_CustomButtonText()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.ShowStepsButtons, true);
                parameters.Add(p => p.NextText, "Continue");
                parameters.Add(p => p.PreviousText, "Back");
            });

            Assert.Contains("Continue", component.Markup);
            Assert.Contains("Back", component.Markup);
        }
    }
}

