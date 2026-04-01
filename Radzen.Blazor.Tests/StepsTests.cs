using Bunit;
using Microsoft.AspNetCore.Components;
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

        [Fact]
        public void Steps_NextButton_DisabledWhenNextStepIsDisabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.ShowStepsButtons, true);
                parameters.Add(p => p.SelectedIndex, 0);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(2);
                    builder.AddAttribute(3, "Text", "Step 2");
                    builder.AddAttribute(4, "Disabled", true);
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(5);
                    builder.AddAttribute(6, "Text", "Step 3");
                    builder.CloseComponent();
                });
            });

            var nextButton = component.Find("button.rz-steps-next");
            Assert.Contains("rz-state-disabled", nextButton.GetAttribute("class"));
            Assert.NotNull(nextButton.GetAttribute("disabled"));
        }

        [Fact]
        public void Steps_PrevButton_DisabledWhenPrevStepIsDisabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.ShowStepsButtons, true);
                parameters.Add(p => p.SelectedIndex, 2);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(2);
                    builder.AddAttribute(3, "Text", "Step 2");
                    builder.AddAttribute(4, "Disabled", true);
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(5);
                    builder.AddAttribute(6, "Text", "Step 3");
                    builder.CloseComponent();
                });
            });

            var prevButton = component.Find("button.rz-steps-prev");
            Assert.Contains("rz-state-disabled", prevButton.GetAttribute("class"));
            Assert.NotNull(prevButton.GetAttribute("disabled"));
        }

        [Fact]
        public void Steps_NextButton_NotDisabledWhenNextStepIsEnabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.ShowStepsButtons, true);
                parameters.Add(p => p.SelectedIndex, 0);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(2);
                    builder.AddAttribute(3, "Text", "Step 2");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(4);
                    builder.AddAttribute(5, "Text", "Step 3");
                    builder.CloseComponent();
                });
            });

            var nextButton = component.Find("button.rz-steps-next");
            Assert.DoesNotContain("rz-state-disabled", nextButton.GetAttribute("class"));
        }

        [Fact]
        public void Steps_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-steps", component.Markup);
        }

        [Fact]
        public void Steps_Renders_SelectedIndex()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.SelectedIndex, 1);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(2);
                    builder.AddAttribute(3, "Text", "Step 2");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-steps-current", component.Markup);
        }

        [Fact]
        public void Steps_Renders_StepTexts()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Registration");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(2);
                    builder.AddAttribute(3, "Text", "Payment");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenStepsItem>(4);
                    builder.AddAttribute(5, "Text", "Confirmation");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Registration", component.Markup);
            Assert.Contains("Payment", component.Markup);
            Assert.Contains("Confirmation", component.Markup);
        }

        [Fact]
        public void Steps_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Style, "width:100%");
            });

            Assert.Contains("width:100%", component.Markup);
        }

        [Fact]
        public void Steps_Transition_None_DoesNotRenderAnimationClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Transition, StepsTransition.None);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            Assert.DoesNotContain("rz-steps-fade", component.Markup);
            Assert.DoesNotContain("rz-steps-slide", component.Markup);
        }

        [Fact]
        public void Steps_Transition_Fade_RendersAnimationClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Transition, StepsTransition.Fade);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-steps-fade", component.Markup);
        }

        [Fact]
        public void Steps_Transition_Slide_RendersAnimationClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Transition, StepsTransition.Slide);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-steps-slide", component.Markup);
        }

        [Fact]
        public void Steps_Transition_Fade_RendersTransitionDurationStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Transition, StepsTransition.Fade);
                parameters.Add(p => p.TransitionDuration, 500);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("--rz-steps-transition-duration: 500ms", component.Markup);
        }

        [Fact]
        public void Steps_Transition_None_DoesNotRenderTransitionDurationStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Transition, StepsTransition.None);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            Assert.DoesNotContain("--rz-steps-transition-duration", component.Markup);
        }

        [Fact]
        public void Steps_Transition_DefaultDuration_Is300()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.Transition, StepsTransition.Fade);
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("--rz-steps-transition-duration: 300ms", component.Markup);
        }
    }
}


