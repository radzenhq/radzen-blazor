using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ProgressBarCircularTests
    {
        [Fact]
        public void ProgressBarCircular_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>();

            Assert.Contains("rz-progressbar-circular", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_SvgElement()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>();

            Assert.Contains("rz-progressbar-circular-viewbox", component.Markup);
            Assert.Contains("rz-progressbar-circular-background", component.Markup);
            Assert.Contains("rz-progressbar-circular-value", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_DeterminateMode_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>();

            Assert.Contains("rz-progressbar-determinate", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_IndeterminateMode()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Mode, ProgressBarMode.Indeterminate);
            });

            Assert.Contains("rz-progressbar-indeterminate", component.Markup);
            Assert.DoesNotContain("rz-progressbar-determinate", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_MediumSize_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>();

            Assert.Contains("rz-progressbar-circular-md", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_SmallSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Size, ProgressBarCircularSize.Small);
            });

            Assert.Contains("rz-progressbar-circular-sm", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_LargeSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Size, ProgressBarCircularSize.Large);
            });

            Assert.Contains("rz-progressbar-circular-lg", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_ExtraSmallSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Size, ProgressBarCircularSize.ExtraSmall);
            });

            Assert.Contains("rz-progressbar-circular-xs", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_ProgressBarStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.ProgressBarStyle, ProgressBarStyle.Success);
            });

            Assert.Contains("rz-progressbar-success", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_Value()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Value, 50);
                parameters.Add(p => p.ShowValue, true);
            });

            Assert.Contains(@"aria-valuenow=""50""", component.Markup);
            Assert.Contains("50%", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_ShowValue_False_HidesLabel()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Value, 50);
                parameters.Add(p => p.ShowValue, false);
            });

            Assert.DoesNotContain("rz-progressbar-circular-label", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_AriaAttributes()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Value, 25);
                parameters.Add(p => p.Min, 0);
                parameters.Add(p => p.Max, 100);
                parameters.Add(p => p.AriaLabel, "Loading progress");
            });

            Assert.Contains(@"role=""progressbar""", component.Markup);
            Assert.Contains(@"aria-valuenow=""25""", component.Markup);
            Assert.Contains(@"aria-valuemin=""0""", component.Markup);
            Assert.Contains(@"aria-valuemax=""100""", component.Markup);
            Assert.Contains(@"aria-label=""Loading progress""", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_CustomUnit()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Value, 50);
                parameters.Add(p => p.Unit, "MB");
                parameters.Add(p => p.ShowValue, true);
            });

            Assert.Contains("50MB", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_Template()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Value, 75);
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Template, builder => builder.AddContent(0, "Done: 75%"));
            });

            Assert.Contains("Done: 75%", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-progressbar-circular", component.Markup);
        }

        [Fact]
        public void ProgressBarCircular_Renders_InfoStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBarCircular>(parameters =>
            {
                parameters.Add(p => p.ProgressBarStyle, ProgressBarStyle.Info);
            });

            Assert.Contains("rz-progressbar-info", component.Markup);
        }
    }
}
