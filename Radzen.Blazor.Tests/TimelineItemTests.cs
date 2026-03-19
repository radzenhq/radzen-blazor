using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TimelineItemTests
    {
        [Fact]
        public void TimelineItem_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>();

            Assert.Contains("rz-timeline-item", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.Text, "Event occurred");
            });

            Assert.Contains("Event occurred", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_LabelParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.Label, "Jan 2024");
            });

            Assert.Contains("Jan 2024", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.AddChildContent("<p>Detailed description</p>");
            });

            Assert.Contains("Detailed description", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>();

            Assert.Contains("rz-timeline-point", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointStyle, PointStyle.Primary);
            });

            Assert.Contains("rz-timeline-point-primary", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointStyle_Success()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointStyle, PointStyle.Success);
            });

            Assert.Contains("rz-timeline-point-success", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointVariant()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointVariant, Variant.Outlined);
            });

            Assert.Contains("rz-timeline-point-outlined", component.Markup);
        }

        [Fact]
        public void TimelineItem_DefaultPointVariant_IsFilled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>();

            Assert.Contains("rz-timeline-point-filled", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointSize_Small()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointSize, PointSize.Small);
            });

            Assert.Contains("rz-timeline-axis-sm", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointSize_Large()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointSize, PointSize.Large);
            });

            Assert.Contains("rz-timeline-axis-lg", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointSize_ExtraSmall()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointSize, PointSize.ExtraSmall);
            });

            Assert.Contains("rz-timeline-axis-xs", component.Markup);
        }

        [Fact]
        public void TimelineItem_DefaultPointSize_IsMedium()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>();

            Assert.Contains("rz-timeline-axis-md", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointShadow()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointShadow, 5);
            });

            Assert.Contains("rz-shadow-5", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_LabelContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.LabelContent, (Microsoft.AspNetCore.Components.RenderFragment)(builder =>
                {
                    builder.AddContent(0, "Custom Label");
                }));
            });

            Assert.Contains("Custom Label", component.Markup);
        }

        [Fact]
        public void TimelineItem_Renders_PointContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimelineItem>(parameters =>
            {
                parameters.Add(p => p.PointContent, (Microsoft.AspNetCore.Components.RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "i");
                    builder.AddContent(1, "check");
                    builder.CloseElement();
                }));
            });

            Assert.Contains("check", component.Markup);
        }
    }
}
