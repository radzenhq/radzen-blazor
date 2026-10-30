using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public void Steps_TabList_HasOrientationAndAriaLabel()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            var tablist = component.Find("ul[role=tablist]");
            Assert.Equal("horizontal", tablist.GetAttribute("aria-orientation"));
            Assert.Equal("Steps", tablist.GetAttribute("aria-label"));
        }

        [Fact]
        public void Steps_TabList_RendersCustomAriaLabel()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSteps>(parameters =>
            {
                parameters.Add(p => p.TabListAriaLabel, "Checkout steps");
                parameters.Add<RenderFragment>(p => p.Steps, builder =>
                {
                    builder.OpenComponent<RadzenStepsItem>(0);
                    builder.AddAttribute(1, "Text", "Step 1");
                    builder.CloseComponent();
                });
            });

            var tablist = component.Find("ul[role=tablist]");
            Assert.Equal("Checkout steps", tablist.GetAttribute("aria-label"));
        }

        [Fact]
        public void Steps_Tabs_HaveRovingTabindex()
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
                    builder.OpenComponent<RadzenStepsItem>(4);
                    builder.AddAttribute(5, "Text", "Step 3");
                    builder.CloseComponent();
                });
            });

            var tabs = component.FindAll("button[role=tab]");
            Assert.Equal("-1", tabs[0].GetAttribute("tabindex"));
            Assert.Equal("0", tabs[1].GetAttribute("tabindex"));
            Assert.Equal("-1", tabs[2].GetAttribute("tabindex"));
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
    

        [Fact]
        public async Task Steps_AsyncCanChange_WithChangeHandler_DoesNotReenterCanChange()
        {
            using var ctx = new TestContext();
            var host = ctx.RenderComponent<StepsBindHost>();

            await host.InvokeAsync(() => host.FindAll("button[role=tab]")[1].Click());

            Assert.Equal(1, host.Instance.CanChangeCount);
            Assert.Equal(0, host.Instance.Index);

            await host.InvokeAsync(() => host.Instance.Pending[0].SetResult());

            host.WaitForAssertion(() =>
            {
                Assert.Equal(1, host.Instance.Index);
                Assert.Equal(1, host.Instance.ChangeCount);
                Assert.Equal(1, host.Instance.CanChangeCount);
                Assert.Equal("true", host.FindAll("button[role=tab]")[1].GetAttribute("aria-selected"));
            });
        }

        [Fact]
        public async Task Steps_Change_IsRaisedAfterBoundSelectedIndexIsUpdated()
        {
            using var ctx = new TestContext();
            var host = ctx.RenderComponent<StepsBindHost>(parameters => parameters.Add(p => p.AsyncCanChangeCalls, 0));

            await host.InvokeAsync(() => host.FindAll("button[role=tab]")[2].Click());

            Assert.Equal(2, host.Instance.Index);
            Assert.Equal(1, host.Instance.ChangeCount);
            Assert.Equal(2, host.Instance.IndexWhenChangeRaised);
        }

        [Fact]
        public async Task Steps_ProgrammaticSelectedIndex_WithAsyncCanChange_DoesNotReenterAndSelectsStep()
        {
            using var ctx = new TestContext();
            var host = ctx.RenderComponent<StepsBindHost>();

            host.Instance.Index = 2;
            await host.InvokeAsync(host.Instance.Refresh);

            Assert.Equal(0, host.Instance.CanChangeCount);
            Assert.Equal(0, host.Instance.ChangeCount);
            Assert.Equal("true", host.FindAll("button[role=tab]")[2].GetAttribute("aria-selected"));
            Assert.Contains("Three", host.Find("[role=tabpanel]:not([hidden])").TextContent);
        }

        [Fact]
        public async Task Steps_ProgrammaticSelectedIndex_DoesNotRaiseCanChange()
        {
            using var ctx = new TestContext();
            var host = ctx.RenderComponent<StepsBindHost>(parameters => parameters.Add(p => p.AsyncCanChangeCalls, 0));

            host.Instance.Index = 1;
            await host.InvokeAsync(host.Instance.Refresh);

            Assert.Equal(0, host.Instance.CanChangeCount);
            Assert.Equal("true", host.FindAll("button[role=tab]")[1].GetAttribute("aria-selected"));
        }

        class StepsBindHost : ComponentBase
        {
            [Parameter]
            public int AsyncCanChangeCalls { get; set; } = 10;

            public int Index { get; set; }
            public int CanChangeCount { get; private set; }
            public int ChangeCount { get; private set; }
            public int IndexWhenChangeRaised { get; private set; } = -1;
            public List<TaskCompletionSource> Pending { get; } = new();

            public void Refresh() => StateHasChanged();

            Task OnCanChange(StepsCanChangeEventArgs args)
            {
                CanChangeCount++;

                if (CanChangeCount > AsyncCanChangeCalls)
                {
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource();
                Pending.Add(tcs);
                return tcs.Task;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenSteps>(0);
                builder.AddAttribute(1, nameof(RadzenSteps.SelectedIndex), Index);
                builder.AddAttribute(2, nameof(RadzenSteps.SelectedIndexChanged), EventCallback.Factory.Create<int>(this, value => Index = value));
                builder.AddAttribute(3, nameof(RadzenSteps.Change), EventCallback.Factory.Create<int>(this, _ => { ChangeCount++; IndexWhenChangeRaised = Index; }));
                builder.AddAttribute(4, nameof(RadzenSteps.CanChange), EventCallback.Factory.Create<StepsCanChangeEventArgs>(this, OnCanChange));
                builder.AddAttribute(5, nameof(RadzenSteps.ShowStepsButtons), false);
                builder.AddAttribute(6, nameof(RadzenSteps.Steps), (RenderFragment)(steps =>
                {
                    var seq = 0;
                    foreach (var text in new[] { "One", "Two", "Three" })
                    {
                        steps.OpenComponent<RadzenStepsItem>(seq++);
                        steps.AddAttribute(seq++, "Text", text);
                        steps.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(0, text)));
                        steps.CloseComponent();
                    }
                }));
                builder.CloseComponent();
            }
        }
    }
}
