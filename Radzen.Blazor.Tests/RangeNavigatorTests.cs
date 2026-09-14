using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
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

        [Fact]
        public void RangeNavigator_TooltipPoints_OnePerItem_Ordered()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx, parameters =>
                parameters.Add(p => p.TooltipFormatString, "{0:N1}"));

            var points = component.Instance.GetTooltipPoints();

            Assert.Equal(new[] { "A: 10.0", "B: 20.0", "C: 15.0" }, points.Select(Describe));
            Assert.True(points[0].X < points[1].X && points[1].X < points[2].X);
            Assert.All(points, p => Assert.InRange(p.X, 0, 1));
        }

        [Fact]
        public void RangeNavigator_TooltipPoints_PlacedOnTheLine()
        {
            using var ctx = CreateChartContext();

            var points = RenderNavigatorWithLineSeries(ctx).Instance.GetTooltipPoints();

            Assert.All(points, p => Assert.InRange(p.Y, 0, 1));
            Assert.True(points[1].Y < points[2].Y && points[2].Y < points[0].Y);
        }

        [Fact]
        public void RangeNavigator_TooltipPoints_TakeTheSeriesColor()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
                parameters.AddChildContent<RadzenRangeNavigatorLineSeries<DataItem>>(series =>
                {
                    series.Add(p => p.Data, SampleData);
                    series.Add(p => p.CategoryProperty, nameof(DataItem.Category));
                    series.Add(p => p.ValueProperty, nameof(DataItem.Value));
                    series.Add(p => p.Stroke, "#1E88E5");
                }));

            Assert.All(component.Instance.GetTooltipPoints(), p => Assert.Equal("#1E88E5", p.Color));
        }

        [Fact]
        public void RangeNavigator_TooltipPoints_DateCategory_UsesHandleLabelFormat()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithDateSeries(ctx, parameters =>
            {
                parameters.Add(p => p.HandleLabelFormatString, "{0:yyyy-MM-dd}");
                parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            });

            var points = component.Instance.GetTooltipPoints();

            Assert.Equal(new[] { "2024-01-01: 3.00", "2024-01-02: 5.00" }, points.Select(Describe));
        }

        [Fact]
        public void RangeNavigator_TooltipPoints_EmptyForCustomSeriesWithoutGetDataPoints()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenRangeNavigator>();
            component.Instance.AddSeries(new CustomSeries());

            Assert.Empty(component.Instance.GetTooltipPoints());
        }

        [Fact]
        public void RangeNavigator_ShowTooltip_SendsPoints()
        {
            using var ctx = CreateChartContext();

            RenderNavigatorWithLineSeries(ctx, parameters =>
            {
                parameters.Add(p => p.ShowTooltip, true);
                parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            });

            Assert.Single(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.updateRangeNavigatorTooltip");
            Assert.Equal(new[] { "A: 10.00", "B: 20.00", "C: 15.00" }, LastSentTexts(ctx));
        }

        [Fact]
        public void RangeNavigator_ShowTooltip_EnabledLater_SendsPoints()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx);
            component.SetParametersAndRender(parameters => parameters.Add(p => p.ShowTooltip, true));

            Assert.Contains(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.updateRangeNavigatorTooltip");
        }

        [Fact]
        public void RangeNavigator_TooltipPoints_SkipsPointsWithoutAPosition()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx);
            component.Instance.AddSeries(new CustomSeriesWithPoints(new Point { X = double.NaN, Y = 1 }));

            Assert.Equal(3, component.Instance.GetTooltipPoints().Count);
        }

        [Fact]
        public void RangeNavigator_TooltipValue_UsesCulture()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx, parameters =>
            {
                parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("de-DE"));
                parameters.Add(p => p.TooltipFormatString, "{0:N1}");
            });

            Assert.Equal("A: 10,0", Describe(component.Instance.GetTooltipPoints()[0]));
        }

        [Fact]
        public async Task RangeNavigator_ChartWrapper_ForwardsTooltipParameters()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenChartRangeNavigator>(n => n
                    .Add(x => x.ShowTooltip, true)
                    .Add(x => x.TooltipFormatString, "{0:N1}")));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var navigator = chart.FindComponent<RadzenRangeNavigator>().Instance;
            Assert.True(navigator.ShowTooltip);
            Assert.Equal("{0:N1}", navigator.TooltipFormatString);
        }

        [Fact]
        public void RangeNavigator_ShowTooltip_RendersTooltipElement()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx, parameters => parameters.Add(p => p.ShowTooltip, true));

            Assert.Single(component.FindAll(".rz-range-nav-tooltip .rz-chart-tooltip.rz-top-chart-tooltip .rz-chart-tooltip-title"));
            Assert.Single(component.FindAll(".rz-range-nav-tooltip .rz-chart-tooltip-item-value"));
            Assert.Single(component.FindAll(".rz-range-nav-tooltip .rz-active-point .rz-active-point-dot"));
        }

        [Fact]
        public void RangeNavigator_ShowTooltip_DefaultFalse()
        {
            using var ctx = CreateChartContext();

            var component = RenderNavigatorWithLineSeries(ctx);

            Assert.False(component.Instance.ShowTooltip);
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.updateRangeNavigatorTooltip");
        }

        [Fact]
        public void RangeNavigator_Tooltip_RefreshesFormattingAndClearsDisabledPoints()
        {
            using var ctx = CreateChartContext();
            var component = RenderNavigatorWithLineSeries(ctx, p =>
            {
                p.Add(x => x.ShowTooltip, true);
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
            });
            Assert.Equal("A: 10.00", LastSentTexts(ctx)[0]);

            component.SetParametersAndRender(p => p.Add(x => x.TooltipFormatString, "{0:N1}"));
            Assert.Equal("A: 10.0", LastSentTexts(ctx)[0]);

            component.SetParametersAndRender(p => p.Add(x => x.Culture, CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal("A: 10,0", LastSentTexts(ctx)[0]);

            component.SetParametersAndRender(p => p.Add(x => x.ShowTooltip, false));
            Assert.Empty(LastSentTexts(ctx));
            component.SetParametersAndRender(p => p.Add(x => x.ShowTooltip, true));
            Assert.Equal("A: 10,0", LastSentTexts(ctx)[0]);
        }

        private static IRenderedComponent<RadzenRangeNavigator> RenderNavigatorWithLineSeries(TestContext ctx,
            Action<ComponentParameterCollectionBuilder<RadzenRangeNavigator>> configure = null)
        {
            return RenderNavigator(ctx, SampleData, nameof(DataItem.Category), nameof(DataItem.Value), configure);
        }

        private static IRenderedComponent<RadzenRangeNavigator> RenderNavigatorWithDateSeries(TestContext ctx,
            Action<ComponentParameterCollectionBuilder<RadzenRangeNavigator>> configure = null)
        {
            var data = new[]
            {
                new DateItem { Date = new DateTime(2024, 1, 1), Value = 3 },
                new DateItem { Date = new DateTime(2024, 1, 2), Value = 5 },
            };

            return RenderNavigator(ctx, data, nameof(DateItem.Date), nameof(DateItem.Value), configure);
        }

        private static IRenderedComponent<RadzenRangeNavigator> RenderNavigator<TItem>(TestContext ctx, IEnumerable<TItem> data,
            string categoryProperty, string valueProperty, Action<ComponentParameterCollectionBuilder<RadzenRangeNavigator>> configure)
        {
            return ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                configure?.Invoke(parameters);
                parameters.AddChildContent<RadzenRangeNavigatorLineSeries<TItem>>(series =>
                {
                    series.Add(p => p.Data, data);
                    series.Add(p => p.CategoryProperty, categoryProperty);
                    series.Add(p => p.ValueProperty, valueProperty);
                });
            });
        }

        private static string Describe(RadzenRangeNavigator.TooltipPoint point) => $"{point.Category}: {point.Value}";

        private static string[] LastSentTexts(TestContext ctx)
        {
            var last = ctx.JSInterop.Invocations.Last(i => i.Identifier == "Radzen.updateRangeNavigatorTooltip");
            return Assert.IsAssignableFrom<IEnumerable<RadzenRangeNavigator.TooltipPoint>>(last.Arguments[1]).Select(Describe).ToArray();
        }

        private class DateItem
        {
            public DateTime Date { get; set; }
            public double Value { get; set; }
        }

        private class CustomSeriesWithPoints(params Point[] points) : CustomSeries, IRangeNavigatorSeries
        {
            public IEnumerable<Point> GetDataPoints(ScaleBase categoryScale) => points;
        }

        private class CustomSeries : IRangeNavigatorSeries
        {
            public ScaleBase TransformCategoryScale(ScaleBase scale) => scale;

            public ScaleBase TransformValueScale(ScaleBase scale) => scale;

            public RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale) => _ => { };
        }
    }
}
