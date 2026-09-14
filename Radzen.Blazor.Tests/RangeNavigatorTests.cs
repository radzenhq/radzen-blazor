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

        [Fact]
        public void RangeNavigator_WithLineSeries_SkipsSeries_BeforeWidthIsMeasured()
        {
            // Rendering the series against the unmeasured scales used to throw (#2693).
            using var ctx = CreateChartContext(navigatorWidth: 0, navigatorHeight: 0);

            var component = RenderNavigatorWithLineSeries(ctx);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Start, 0.1));

            Assert.DoesNotContain("rz-range-nav-series", component.Markup);
        }

        [Fact]
        public void RangeNavigator_WithLineSeries_RendersSeries_AfterWidthIsMeasured()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx);

            Assert.Contains("rz-range-nav-series", component.Markup);
            Assert.DoesNotContain("NaN", component.Markup);
        }

        [Fact]
        public void RangeNavigator_WithLineSeries_UpdatesScales_WhenSeriesDataChanges()
        {
            // The scales used to keep the domain of the data first seen until a resize (#2713).
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx);

            Assert.Equal(10, component.Instance.ValueScale.Input.Start);
            Assert.Equal(20, component.Instance.ValueScale.Input.End);

            component.SetParametersAndRender(parameters =>
                parameters.AddChildContent<RadzenRangeNavigatorLineSeries<DataItem>>(series =>
                {
                    series.Add(p => p.Data, WiderData);
                    series.Add(p => p.CategoryProperty, nameof(DataItem.Category));
                    series.Add(p => p.ValueProperty, nameof(DataItem.Value));
                }));

            Assert.Equal(100, component.Instance.ValueScale.Input.Start);
            Assert.Equal(300, component.Instance.ValueScale.Input.End);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RangeNavigator_Rerenders_WhenDataChangesWithinTheSameDomain(bool renameCategories)
        {
            using var ctx = CreateChartContext();
            var component = RenderNavigatorWithLineSeries(ctx);
            component.SetParametersAndRender(p => p.Add(x => x.ShowAxis, true));
            var data = new[]
            {
                new DataItem { Category = renameCategories ? "X" : "A", Value = 10 },
                new DataItem { Category = renameCategories ? "Y" : "B", Value = 20 },
                new DataItem { Category = renameCategories ? "Z" : "C", Value = renameCategories ? 15 : 12 },
            };

            component.SetParametersAndRender(parameters =>
                parameters.AddChildContent<RadzenRangeNavigatorLineSeries<DataItem>>(series =>
                {
                    series.Add(p => p.Data, data);
                    series.Add(p => p.CategoryProperty, nameof(DataItem.Category));
                    series.Add(p => p.ValueProperty, nameof(DataItem.Value));
                }));

            Assert.Equal(10, component.Instance.ValueScale.Input.Start);
            Assert.Equal(20, component.Instance.ValueScale.Input.End);
            if (renameCategories)
            {
                Assert.DoesNotContain(">A<", component.Markup);
                Assert.Contains(">X<", component.Markup);
            }
            else
            {
                Assert.Contains("166.66666666666669 160 ", component.Markup);
                Assert.DoesNotContain("166.66666666666669 100 ", component.Markup);
            }
            var renders = component.RenderCount;
            component.SetParametersAndRender(p => p.Add(x => x.Start, 0.2));
            Assert.Equal(renders + 1, component.RenderCount);
        }

        private static DataItem[] WiderData => new[]
        {
            new DataItem { Category = "A", Value = 100 },
            new DataItem { Category = "B", Value = 300 },
            new DataItem { Category = "C", Value = 200 },
        };

        private static IRenderedComponent<RadzenRangeNavigator> RenderNavigatorWithLineSeries(TestContext ctx)
        {
            return ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
                parameters.AddChildContent<RadzenRangeNavigatorLineSeries<DataItem>>(series =>
                {
                    series.Add(p => p.Data, SampleData);
                    series.Add(p => p.CategoryProperty, nameof(DataItem.Category));
                    series.Add(p => p.ValueProperty, nameof(DataItem.Value));
                }));
        }
    }
}
