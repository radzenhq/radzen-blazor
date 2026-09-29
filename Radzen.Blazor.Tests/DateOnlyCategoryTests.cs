using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DateOnlyCategoryTests
    {
        class DateTimeItem
        {
            public DateTime Date { get; set; }
            public double Value { get; set; }
        }

        class DateOnlyItem
        {
            public DateOnly Date { get; set; }
            public double Value { get; set; }
        }

        class NullableDateOnlyItem
        {
            public DateOnly? Date { get; set; }
            public double Value { get; set; }
        }

        class NullableDateTimeItem
        {
            public DateTime? Date { get; set; }
            public double Value { get; set; }
        }

        class DateTimeOffsetItem
        {
            public DateTimeOffset Date { get; set; }
            public double Value { get; set; }
        }

        static readonly DateOnly Start = new(2025, 1, 1);

        static List<DateTimeItem> DateTimeData() => Enumerable.Range(0, 365)
            .Select(i => new DateTimeItem { Date = Start.AddDays(i).ToDateTime(TimeOnly.MinValue), Value = 100 + i % 7 }).ToList();

        static List<DateOnlyItem> DateOnlyData() => Enumerable.Range(0, 365)
            .Select(i => new DateOnlyItem { Date = Start.AddDays(i), Value = 100 + i % 7 }).ToList();

        static List<NullableDateOnlyItem> NullableDateOnlyData() => Enumerable.Range(0, 365)
            .Select(i => new NullableDateOnlyItem { Date = Start.AddDays(i), Value = 100 + i % 7 }).ToList();

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Setup<Rect>("Radzen.createChart", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 400 });
            ctx.JSInterop.Setup<double[]>("Radzen.createRangeNavigator", _ => true)
                .SetResult(new double[] { 800, 60 });
            ctx.Services.AddScoped<TooltipService>();
            return ctx;
        }

        static IRenderedComponent<RadzenChart> RenderChart<TItem>(TestContext ctx, IEnumerable<TItem> data, Action<ComponentParameterCollectionBuilder<RadzenChart>> configure = null, Action<ComponentParameterCollectionBuilder<RadzenCategoryAxis>> axis = null)
        {
            return ctx.RenderComponent<RadzenChart>(parameters =>
            {
                configure?.Invoke(parameters);
                parameters.AddChildContent<RadzenLineSeries<TItem>>(series => series
                    .Add(p => p.Data, data)
                    .Add(p => p.CategoryProperty, "Date")
                    .Add(p => p.ValueProperty, "Value"));
                parameters.AddChildContent<RadzenCategoryAxis>(a =>
                {
                    a.Add(p => p.FormatString, "{0:MMM dd}");
                    axis?.Invoke(a);
                });
            });
        }

        static List<string> CategoryLabels(IRenderedFragment chart)
        {
            return chart.FindAll("g.rz-category-axis .rz-tick-text").Select(e => e.TextContent).ToList();
        }

        [Fact]
        public void DateScale_WholeDays_TicksNeverCloserThanADay()
        {
            var start = new DateTime(2025, 7, 1);
            var end = new DateTime(2025, 7, 3);

            var hourly = new DateScale { Input = new ScaleRange { Start = start.Ticks, End = end.Ticks }, Output = new ScaleRange { Start = 0, End = 1400 } };
            var daily = new DateScale { WholeDays = true, Input = new ScaleRange { Start = start.Ticks, End = end.Ticks }, Output = new ScaleRange { Start = 0, End = 1400 } };

            var hourlyTicks = hourly.TickValues(100).Select(t => new DateTime((long)t)).ToList();
            var dailyTicks = daily.TickValues(100).Select(t => new DateTime((long)t)).ToList();

            Assert.True(hourlyTicks.Count > 3);
            Assert.Equal(3, dailyTicks.Count);
            Assert.All(dailyTicks, t => Assert.Equal(TimeSpan.Zero, t.TimeOfDay));
        }

        [Fact]
        public void DateScale_WholeDays_ValueIsTheDay()
        {
            var scale = new DateScale { WholeDays = true };

            var value = scale.Value(new DateTime(2025, 3, 5, 13, 45, 0).Ticks);

            Assert.Equal(new DateOnly(2025, 3, 5), value);
            Assert.IsType<DateTime>(new DateScale().Value(new DateTime(2025, 3, 5, 13, 45, 0).Ticks));
        }

        [Fact]
        public void DateScale_Resize_AcceptsDateOnly()
        {
            var scale = new DateScale();

            scale.Resize(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

            Assert.Equal(new DateTime(2025, 1, 1).Ticks, scale.Input.Start);
            Assert.Equal(new DateTime(2025, 12, 31).Ticks, scale.Input.End);
        }

        [Fact]
        public void DateScale_FormatTick_DefaultFormatsDateOnly()
        {
            var scale = new DateScale { WholeDays = true };
            var day = new DateOnly(2025, 3, 5);

            Assert.Equal(day.ToShortDateString(), scale.FormatTick("", day));
            Assert.Equal("Mar 05", scale.FormatTick("{0:MMM dd}", day));
        }

        [Fact]
        public void Chart_DateOnlyCategories_UseDateScale_WithTheSameTicksAsDateTime()
        {
            using var ctx = CreateContext();

            var dateTimeChart = RenderChart(ctx, DateTimeData());
            var dateOnlyChart = RenderChart(ctx, DateOnlyData());

            var expected = CategoryLabels(dateTimeChart);

            Assert.IsType<DateScale>(dateOnlyChart.Instance.CategoryScale);
            Assert.True(((DateScale)dateOnlyChart.Instance.CategoryScale).WholeDays);
            Assert.InRange(expected.Count, 2, 20);
            Assert.Equal(expected, CategoryLabels(dateOnlyChart));
        }

        [Fact]
        public void Chart_NullableDateOnlyCategories_UseDateScale()
        {
            using var ctx = CreateContext();

            var dateTimeChart = RenderChart(ctx, DateTimeData());
            var nullableChart = RenderChart(ctx, NullableDateOnlyData());

            Assert.IsType<DateScale>(nullableChart.Instance.CategoryScale);
            Assert.Equal(CategoryLabels(dateTimeChart), CategoryLabels(nullableChart));
        }

        [Fact]
        public void Chart_DateTimeOffsetCategories_UseDateScale()
        {
            using var ctx = CreateContext();

            var data = Enumerable.Range(0, 365)
                .Select(i => new DateTimeOffsetItem { Date = new DateTimeOffset(Start.AddDays(i).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), Value = 100 + i % 7 }).ToList();

            var dateTimeChart = RenderChart(ctx, DateTimeData());
            var offsetChart = RenderChart(ctx, data);

            Assert.IsType<DateScale>(offsetChart.Instance.CategoryScale);
            Assert.False(((DateScale)offsetChart.Instance.CategoryScale).WholeDays);
            Assert.Equal(CategoryLabels(dateTimeChart), CategoryLabels(offsetChart));
        }

        [Fact]
        public void Chart_DateOnlyCategories_FormatterReceivesDateOnly()
        {
            using var ctx = CreateContext();
            var received = new List<object>();

            var chart = RenderChart(ctx, DateOnlyData(), axis: a => a.Add(p => p.Formatter, value =>
            {
                received.Add(value);
                return ((DateOnly)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }));

            Assert.NotEmpty(received);
            Assert.All(received, value => Assert.IsType<DateOnly>(value));
            Assert.Equal("2025-01-01", CategoryLabels(chart).First());
        }

        [Fact]
        public void Chart_DateOnlyCategories_ZoomedBelowADay_DoesNotRepeatTheDay()
        {
            using var ctx = CreateContext();

            var chart = RenderChart(ctx, DateOnlyData(), configure: p => p
                .Add(x => x.AllowZoom, true)
                .Add(x => x.ViewStart, 0.5)
                .Add(x => x.ViewEnd, 0.503));

            var labels = CategoryLabels(chart);

            Assert.NotEmpty(labels);
            Assert.Equal(labels.Distinct().Count(), labels.Count);
        }

        [Fact]
        public void Chart_MixedDateOnlyAndDateTimeSeries_KeepsTimeResolution()
        {
            using var ctx = CreateContext();

            var chart = ctx.RenderComponent<RadzenChart>(parameters =>
            {
                parameters.AddChildContent<RadzenLineSeries<DateOnlyItem>>(series => series
                    .Add(p => p.Data, DateOnlyData())
                    .Add(p => p.CategoryProperty, "Date")
                    .Add(p => p.ValueProperty, "Value"));
                parameters.AddChildContent<RadzenLineSeries<DateTimeItem>>(series => series
                    .Add(p => p.Data, DateTimeData())
                    .Add(p => p.CategoryProperty, "Date")
                    .Add(p => p.ValueProperty, "Value"));
            });

            var scale = Assert.IsType<DateScale>(chart.Instance.CategoryScale);
            Assert.False(scale.WholeDays);
        }

        [Fact]
        public void Chart_NullDateOnlyCategory_ThrowsLikeNullDateTime()
        {
            using var ctx = CreateContext();

            var dateTimeData = new[] { new NullableDateTimeItem { Date = new DateTime(2025, 1, 1), Value = 1 }, new NullableDateTimeItem { Date = null, Value = 2 } };
            var dateOnlyData = new[] { new NullableDateOnlyItem { Date = new DateOnly(2025, 1, 1), Value = 1 }, new NullableDateOnlyItem { Date = null, Value = 2 } };

            var expected = Assert.ThrowsAny<Exception>(() => RenderChart(ctx, dateTimeData));
            var actual = Assert.ThrowsAny<Exception>(() => RenderChart(ctx, dateOnlyData));

            Assert.Equal(expected.GetType(), actual.GetType());
        }

        static IRenderedComponent<RadzenRangeNavigator> RenderNavigator<TItem>(TestContext ctx, IEnumerable<TItem> data, Action<ComponentParameterCollectionBuilder<RadzenRangeNavigator>> configure = null)
        {
            return ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                parameters.Add(p => p.ShowAxis, true);
                parameters.Add(p => p.AxisFormatString, "{0:MMM}");
                configure?.Invoke(parameters);
                parameters.AddChildContent<RadzenRangeNavigatorLineSeries<TItem>>(series => series
                    .Add(p => p.Data, data)
                    .Add(p => p.CategoryProperty, "Date")
                    .Add(p => p.ValueProperty, "Value"));
            });
        }

        [Fact]
        public void RangeNavigator_DateOnlySeries_UsesDateScale_WithTheSameTicksAsDateTime()
        {
            using var ctx = CreateContext();

            var dateTimeNavigator = RenderNavigator(ctx, DateTimeData());
            var dateOnlyNavigator = RenderNavigator(ctx, DateOnlyData());

            var expected = dateTimeNavigator.Instance.GetAxisTicks().Select(t => t.Label).ToList();

            Assert.IsType<DateScale>(dateOnlyNavigator.Instance.CategoryScale);
            Assert.InRange(expected.Count, 2, 20);
            Assert.Equal(expected, dateOnlyNavigator.Instance.GetAxisTicks().Select(t => t.Label).ToList());
            Assert.Equal(dateTimeNavigator.Instance.GetHandleLabel(0.5), dateOnlyNavigator.Instance.GetHandleLabel(0.5));
            Assert.Equal("07/02/2025", dateOnlyNavigator.Instance.GetHandleLabel(0.5));
        }

        [Fact]
        public void RangeNavigator_DateOnlySeries_HandleLabelFormatterReceivesDateOnly()
        {
            using var ctx = CreateContext();

            var navigator = RenderNavigator(ctx, DateOnlyData(), p => p.Add(x => x.HandleLabelFormatter, value =>
            {
                Assert.IsType<DateOnly>(value);
                return ((DateOnly)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }));

            Assert.Equal("2025-01-01", navigator.Instance.GetHandleLabel(0));
            Assert.Equal("2025-12-31", navigator.Instance.GetHandleLabel(1));
        }

        [Fact]
        public void RangeNavigator_DateOnlyMinMax_WithoutSeries_RendersDateAxis()
        {
            using var ctx = CreateContext();

            var navigator = ctx.RenderComponent<RadzenRangeNavigator>(parameters =>
            {
                parameters.Add(p => p.ShowAxis, true);
                parameters.Add(p => p.AxisFormatString, "{0:MMM}");
                parameters.Add(p => p.Min, new DateOnly(2025, 1, 1));
                parameters.Add(p => p.Max, new DateOnly(2025, 12, 31));
            });

            var scale = Assert.IsType<DateScale>(navigator.Instance.CategoryScale);
            Assert.True(scale.WholeDays);
            Assert.InRange(navigator.Instance.GetAxisTicks().Count, 2, 20);
            Assert.Equal("01/01/2025", navigator.Instance.GetHandleLabel(0));
        }
    }
}
