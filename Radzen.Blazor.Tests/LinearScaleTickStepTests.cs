using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class LinearScaleTickStepTests
    {
        private const int TickDistance = 100;

        private static readonly double[] Revenue2023 = { 234000, 269000, 233000, 244000, 214000, 253000, 274000, 284000, 273000, 282000, 289000, 294000 };

        private static readonly double[] Revenue2024 = { 334000, 369000, 333000, 344000, 314000, 353000, 374000, 384000, 373000, 382000, 389000, 394000 };

        private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec" };

        private static DataItem[] Revenue(double[] values)
        {
            return values.Select((value, index) => new DataItem { Category = Months[index], Value = value }).ToArray();
        }

        private static string FormatAsUSD(object value)
        {
            return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
        }

        private static LinearScale Scale(double start, double end, double height)
        {
            return new LinearScale
            {
                Input = new ScaleRange { Start = start, End = end },
                Output = new ScaleRange { Start = height, End = 0 }
            };
        }

        private static int Count((double Start, double End, double Step) ticks)
        {
            return (int)Math.Round((ticks.End - ticks.Start) / ticks.Step) + 1;
        }

        private static (double Start, double End, double Step) PreviousTicks(double start, double end, double height)
        {
            var scale = new LinearScale();
            var ticks = Math.Max(1, Math.Ceiling(height / TickDistance));
            var step = scale.NiceNumber(scale.NiceNumber(end - start, false) / ticks, true);

            return (Math.Floor(start / step) * step, Math.Ceiling(end / step) * step, step);
        }

        public static IEnumerable<object[]> SpreadOfRanges()
        {
            var random = new Random(2026);

            for (var index = 0; index < 50; index++)
            {
                var span = Math.Round(1 + random.NextDouble() * 9, 2) * Math.Pow(10, random.Next(-2, 7));
                var offset = Math.Round(random.NextDouble(), 2) * span;

                var (start, end) = (index % 4) switch
                {
                    0 => (0.0, span),
                    1 => (offset, offset + span),
                    2 => (-offset - span, -offset),
                    _ => (-offset, span - offset)
                };

                foreach (var height in new[] { 150, 300, 600 })
                {
                    yield return new object[] { start, end, height };
                }
            }
        }

        [Theory]
        [MemberData(nameof(SpreadOfRanges))]
        public void Ticks_NeverFewerThanWithTheRangeRoundedFirst(double start, double end, double height)
        {
            var ticks = Scale(start, end, height).Ticks(TickDistance);
            var previous = PreviousTicks(start, end, height);

            Assert.True(ticks.Step <= previous.Step, $"step {ticks.Step} > {previous.Step}");
            Assert.True(Count(ticks) >= Count(previous), $"{Count(ticks)} ticks < {Count(previous)}");
        }

        [Theory]
        [InlineData(0, 394000, 250, 0, 400000, 100000)]
        [InlineData(214000, 394000, 250, 200000, 400000, 50000)]
        [InlineData(-394000, 0, 250, -400000, 0, 100000)]
        [InlineData(-394000, -214000, 250, -400000, -200000, 50000)]
        [InlineData(-150000, 394000, 250, -200000, 400000, 200000)]
        [InlineData(-10, 20, 250, -10, 20, 10)]
        [InlineData(0, 90, 500, 0, 100, 20)]
        public void Ticks_StepFollowsTheDataRange(double inputStart, double inputEnd, double height, double start, double end, double step)
        {
            Assert.Equal((start, end, step), Scale(inputStart, inputEnd, height).Ticks(TickDistance));
        }

        [Theory]
        [InlineData(0, 394000, 250, 0, 400000, 200000)]
        [InlineData(-150000, 394000, 250, -500000, 500000, 500000)]
        [InlineData(-10, 20, 250, -20, 20, 20)]
        public void PreviousTicks_RoundedTheRangeFirst(double inputStart, double inputEnd, double height, double start, double end, double step)
        {
            Assert.Equal((start, end, step), PreviousTicks(inputStart, inputEnd, height));
        }

        [Fact]
        public void Ticks_WithMinAndMax_KeepTheExactRange()
        {
            var scale = Scale(0, 0, 250);

            scale.Resize(0, 394000);

            Assert.Equal((0, 394000, 394000 / 3.0), scale.Ticks(TickDistance));
        }

        [Fact]
        public void Ticks_WithMin_KeepTheExactStartAndRoundTheEnd()
        {
            var scale = Scale(214000, 394000, 250);

            scale.Resize(100000, null);

            Assert.Equal((100000, 400000, 100000), scale.Ticks(TickDistance));
        }

        [Fact]
        public void Ticks_WithStep_UseIt()
        {
            var scale = Scale(0, 394000, 250);

            scale.Step = 150000;

            Assert.Equal((0, 450000, 150000), scale.Ticks(TickDistance));
        }

        [Fact]
        public void Ticks_WithMinMaxAndStep_UseThemAsGiven()
        {
            var scale = Scale(0, 0, 250);

            scale.Resize(0, 394000);
            scale.Step = 50000;

            Assert.Equal((0, 394000, 50000), scale.Ticks(TickDistance));
        }

        private static async Task<IRenderedComponent<RadzenChart>> RenderAreaDemoAsync(TestContext ctx, ValueAxisRange range, double width)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "2023")
                    .Add(x => x.RenderingOrder, 1)
                    .Add(x => x.Data, Revenue(Revenue2023)))
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "2024")
                    .Add(x => x.LineType, LineType.Dashed)
                    .Add(x => x.Data, Revenue(Revenue2024)))
                .AddChildContent<RadzenCategoryAxis>(a => a.Add(x => x.LabelAutoRotation, -45))
                .AddChildContent<RadzenValueAxis>(a => a
                    .Add(x => x.Range, range)
                    .Add(x => x.Formatter, FormatAsUSD)
                    .AddChildContent<RadzenGridLines>(g => g.Add(x => x.Visible, true))
                    .AddChildContent<RadzenAxisTitle>(t => t.Add(x => x.Text, "Revenue in USD"))));

            await chart.InvokeAsync(() => chart.Instance.Resize(width, 300));

            return chart;
        }

        private static string ValueAxisLabels(IRenderedComponent<RadzenChart> chart)
        {
            return string.Join("|", chart.FindAll("g.rz-value-axis .rz-tick-text").Select(t => t.TextContent.Trim()));
        }

        [Theory]
        [InlineData(ValueAxisRange.Auto, 324, "$0|$100,000|$200,000|$300,000|$400,000")]
        [InlineData(ValueAxisRange.Auto, 850, "$0|$100,000|$200,000|$300,000|$400,000")]
        [InlineData(ValueAxisRange.Data, 324, "$200,000|$250,000|$300,000|$350,000|$400,000")]
        [InlineData(ValueAxisRange.Data, 850, "$200,000|$250,000|$300,000|$350,000|$400,000")]
        public async Task AreaDemo_ValueAxisSteps(ValueAxisRange range, double width, string labels)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAreaDemoAsync(ctx, range, width);

            Assert.Equal(labels, ValueAxisLabels(chart));
        }
    }
}
