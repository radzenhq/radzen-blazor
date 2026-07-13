using System;
using System.Globalization;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class RangeNavigatorTests
    {
        [Fact]
        public void RangeNavigator_Renders_WithClassName()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>();

            Assert.Contains("rz-range-nav", component.Markup);
        }

        [Fact]
        public void RangeNavigator_DefaultStart_IsZero()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>();

            Assert.Equal(0, component.Instance.Start);
        }

        [Fact]
        public void RangeNavigator_DefaultEnd_IsOne()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>();

            Assert.Equal(1, component.Instance.End);
        }

        [Fact]
        public void RangeNavigator_CustomStartEnd()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                parameters.Add(p => p.Start, 0.25);
                parameters.Add(p => p.End, 0.75);
            });

            Assert.Equal(0.25, component.Instance.Start);
            Assert.Equal(0.75, component.Instance.End);
        }

        [Fact]
        public void RangeNavigator_Renders_WindowElement()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>();

            Assert.Contains("rz-range-nav-window", component.Markup);
        }

        [Fact]
        public void RangeNavigator_ShowHandleLabels_DefaultFalse()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>();

            Assert.False(component.Instance.ShowHandleLabels);
        }

        [Fact]
        public void RangeNavigator_ShowAxis_DefaultFalse()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>();

            Assert.False(component.Instance.ShowAxis);
        }

        [Fact]
        public void RangeNavigator_CustomShowOptions()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                parameters.Add(p => p.ShowHandleLabels, true);
                parameters.Add(p => p.ShowAxis, true);
            });

            Assert.True(component.Instance.ShowHandleLabels);
            Assert.True(component.Instance.ShowAxis);
        }

        [Fact]
        public void RangeNavigator_HandleLabelFormatter_FormatsNumericValues()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                parameters.Add(p => p.HandleLabelFormatter, value => $"#{value}");
                parameters.Add(p => p.HandleLabelFormatString, "{0:N2}");
            });

            component.Instance.CategoryScale = new LinearScale
            {
                Input = new ScaleRange { Start = 0, End = 100 }
            };

            Assert.Equal("#50", component.Instance.GetHandleLabel(0.5));
        }

        [Fact]
        public void RangeNavigator_HandleLabelFormatter_ReceivesDateTimeForDateScale()
        {
            using var ctx = CreateChartContext();

            var start = new DateTime(2024, 1, 1);
            var end = new DateTime(2024, 12, 31);

            var component = ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                parameters.Add(p => p.HandleLabelFormatter, value =>
                {
                    var date = (DateTime)value;
                    return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                });
            });

            component.Instance.CategoryScale = new DateScale
            {
                Input = new ScaleRange { Start = start.Ticks, End = end.Ticks }
            };

            Assert.Equal("2024-01-01", component.Instance.GetHandleLabel(0));
        }

        [Fact]
        public void RangeNavigator_HandleLabelFormatString_UsedWhenNoFormatter()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                parameters.Add(p => p.HandleLabelFormatString, "{0:N2}");
            });

            component.Instance.CategoryScale = new LinearScale
            {
                Input = new ScaleRange { Start = 0, End = 100 }
            };

            Assert.Equal("50.00", component.Instance.GetHandleLabel(0.5));
        }
    }
}
