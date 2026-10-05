using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ValueAxisBoundRoundingTests
    {
        private const int TickDistance = 100;

        private static LinearScale Scale(double start, double end, double height)
        {
            return new LinearScale
            {
                Input = new ScaleRange { Start = start, End = end },
                Output = new ScaleRange { Start = height, End = 0 }
            };
        }

        private static double[] TickValues(LinearScale scale)
        {
            return scale.TickValues(TickDistance).ToArray();
        }

        [Fact]
        public void MinOnly_KeepsTheMinAndRoundsTheEnd()
        {
            var scale = Scale(4210, 13030, 250);

            scale.Resize(0, null);

            Assert.Equal((0, 15000, 5000), scale.Ticks(TickDistance));
            Assert.Equal(new double[] { 0, 5000, 10000, 15000 }, TickValues(scale));
        }

        [Fact]
        public void MinOffTheStep_KeepsTheMinAndPlacesTicksOnMultiplesOfTheStep()
        {
            var scale = Scale(10, 97, 500);

            scale.Resize(3, null);

            Assert.Equal((3, 100, 20), scale.Ticks(TickDistance));
            Assert.Equal(new double[] { 20, 40, 60, 80, 100 }, TickValues(scale));
        }

        [Fact]
        public void MaxOnly_KeepsTheMaxAndRoundsTheStart()
        {
            var scale = Scale(4210, 13030, 250);

            scale.Resize(null, 12000);

            Assert.Equal((4000, 12000, 2000), scale.Ticks(TickDistance));
            Assert.Equal(new double[] { 4000, 6000, 8000, 10000, 12000 }, TickValues(scale));
        }

        [Fact]
        public void MaxOffTheStep_KeepsTheMaxAndPlacesTicksOnMultiplesOfTheStep()
        {
            var scale = Scale(4210, 13030, 250);

            scale.Resize(null, 13500);

            Assert.Equal((0, 13500, 5000), scale.Ticks(TickDistance));
            Assert.Equal(new double[] { 0, 5000, 10000 }, TickValues(scale));
        }

        [Fact]
        public void MinAndMax_AreNotRoundedAndDivideTheRangeEvenly()
        {
            var scale = Scale(4210, 13030, 250);

            scale.Resize(0, 13030);

            Assert.Equal((0, 13030, 13030 / 3.0), scale.Ticks(TickDistance));
            Assert.Collection(TickValues(scale),
                tick => Assert.Equal(0, tick),
                tick => Assert.Equal(13030 / 3.0, tick, 6),
                tick => Assert.Equal(2 * 13030 / 3.0, tick, 6),
                tick => Assert.Equal(13030, tick));
        }

        [Fact]
        public void MinWithStep_IsNotRounded()
        {
            var scale = Scale(4210, 13030, 250);

            scale.Resize(0, null);
            scale.Step = 4000;
            scale.Round = false;

            Assert.Equal((0, 13030, 4000), scale.Ticks(TickDistance));
            Assert.Equal(new double[] { 0, 4000, 8000, 12000 }, TickValues(scale));
        }

        [Fact]
        public void NoBounds_RoundBothEnds()
        {
            var scale = Scale(4210, 13030, 250);

            Assert.Equal((4000, 14000, 2000), scale.Ticks(TickDistance));
            Assert.Equal(new double[] { 4000, 6000, 8000, 10000, 12000, 14000 }, TickValues(scale));
        }

        [Fact]
        public void AllNegativeWithMaxZero_RoundsTheStart()
        {
            var scale = Scale(-27, -12, 250);

            scale.Resize(null, 0);

            Assert.Equal((-30, 0, 10), scale.Ticks(TickDistance));
            Assert.Equal(new double[] { -30, -20, -10, 0 }, TickValues(scale));
        }

        [Fact]
        public void Round_IsTrueWhileEitherEndIsRounded()
        {
            var scale = Scale(4210, 13030, 250);

            scale.Resize(0, null);
            Assert.True(scale.Round);

            scale.Resize(null, 13030);
            Assert.False(scale.Round);

            scale.Round = true;
            Assert.True(scale.RoundStart && scale.RoundEnd);
        }

        [Fact]
        public void FitKeepsTheMinAndTheTicksOnMultiplesOfTheStep()
        {
            var scale = Scale(10, 97, 500);

            scale.Resize(3, null);
            scale.Fit(TickDistance);

            Assert.Equal(3, scale.Input.Start);
            Assert.Equal(100, scale.Input.End);
            Assert.Equal(new double[] { 20, 40, 60, 80, 100 }, TickValues(scale));
        }

        private static readonly DataItem[] Revenue =
        {
            new DataItem { Category = "Q1", Value = 4210 },
            new DataItem { Category = "Q2", Value = 8750 },
            new DataItem { Category = "Q3", Value = 13030 },
            new DataItem { Category = "Q4", Value = 11420 }
        };

        private static async Task<string> ValueAxisLabelsAsync<TSeries>(DataItem[] data, System.Action<ComponentParameterCollectionBuilder<RadzenValueAxis>> axis)
            where TSeries : CartesianSeries<DataItem>
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<TSeries>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenValueAxis>(axis));

            await chart.InvokeAsync(() => chart.Instance.Resize(460, 300));

            return string.Join("|", chart.FindAll("g.rz-value-axis .rz-tick-text").Select(t => t.TextContent.Trim()));
        }

        [Fact]
        public async Task ColumnWithMinZero_GetsTheSameTicksAsWithoutMin()
        {
            Assert.Equal("0|5000|10000|15000", await ValueAxisLabelsAsync<RadzenColumnSeries<DataItem>>(Revenue, a => a.Add(x => x.Min, 0)));
            Assert.Equal("0|5000|10000|15000", await ValueAxisLabelsAsync<RadzenColumnSeries<DataItem>>(Revenue, a => { }));
        }

        [Fact]
        public async Task ColumnWithMinAboveZero_WinsOverTheZeroOfRangeAuto()
        {
            Assert.Equal("5000|10000|15000", await ValueAxisLabelsAsync<RadzenColumnSeries<DataItem>>(Revenue, a => a.Add(x => x.Min, 3000)));
        }

        [Fact]
        public async Task LineWithMaxOnly_RoundsTheStart()
        {
            Assert.Equal("4000|6000|8000|10000|12000", await ValueAxisLabelsAsync<RadzenLineSeries<DataItem>>(Revenue, a => a.Add(x => x.Max, 12000)));
        }

        [Fact]
        public async Task ColumnWithMinAndMax_KeepsBoth()
        {
            Assert.Equal("0|4343.33333333333|8686.66666666666|13030", await ValueAxisLabelsAsync<RadzenColumnSeries<DataItem>>(Revenue, a => a.Add(x => x.Min, 0).Add(x => x.Max, 13030)));
        }

        [Fact]
        public async Task ColumnWithMinAndStep_UsesTheStepFromTheMin()
        {
            Assert.Equal("0|4000|8000|12000", await ValueAxisLabelsAsync<RadzenColumnSeries<DataItem>>(Revenue, a => a.Add(x => x.Min, 0).Add(x => x.Step, 4000)));
        }

        [Fact]
        public async Task AllNegativeLineWithMaxZero_EndsAtZero()
        {
            var data = new[]
            {
                new DataItem { Category = "A", Value = -27 },
                new DataItem { Category = "B", Value = -12 },
                new DataItem { Category = "C", Value = -18 }
            };

            Assert.Equal("-30|-20|-10|0", await ValueAxisLabelsAsync<RadzenLineSeries<DataItem>>(data, a => a.Add(x => x.Max, 0)));
            Assert.Equal("-30|-25|-20|-15|-10", await ValueAxisLabelsAsync<RadzenLineSeries<DataItem>>(data, a => { }));
        }
    }
}
