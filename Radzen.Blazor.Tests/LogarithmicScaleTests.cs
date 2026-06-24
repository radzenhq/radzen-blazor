using System;
using System.Linq;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LogarithmicScaleTests
    {
        [Fact]
        public void Ticks_WithDefaultInput_ReturnsFiniteRange()
        {
            // A scale that received no data keeps the ScaleRange defaults (+Infinity, -Infinity).
            // This happens when the last series is removed from a chart with a logarithmic axis.
            var scale = new LogarithmicScale();

            var (start, end, step) = scale.Ticks(100);

            Assert.True(double.IsFinite(start));
            Assert.True(double.IsFinite(end));
            Assert.True(double.IsFinite(step));
            Assert.True(start > 0);
            Assert.True(end >= start);
            Assert.True(step > 1);
        }

        [Fact]
        public void TickValues_WithDefaultInput_Terminates()
        {
            var scale = new LogarithmicScale();

            var values = scale.TickValues(100).Take(1000).ToList();

            Assert.True(values.Count < 1000);
            Assert.All(values, v => Assert.True(double.IsFinite(v)));
        }

        [Fact]
        public void Ticks_WithData_RoundsToPowersOfBase()
        {
            var scale = new LogarithmicScale
            {
                Input = new ScaleRange { Start = 376000, End = 1400000000 },
                Output = new ScaleRange { Start = 0, End = 400 }
            };

            var (start, end, step) = scale.Ticks(100);

            Assert.Equal(100000, start);
            Assert.Equal(10000000000, end);
            Assert.Equal(10, step);
        }

        [Fact]
        public void AxisMeasurer_YAxis_WithDefaultLogarithmicInput_Terminates()
        {
            // Regression test: AxisMeasurer.YAxis looped forever when the value scale was a
            // LogarithmicScale with non-finite input - it froze the browser tab when navigating
            // away from a page with a logarithmic chart.
            var scale = new LogarithmicScale
            {
                Input = new ScaleRange { Start = double.PositiveInfinity, End = double.NegativeInfinity }
            };

            var size = AxisMeasurer.YAxis(scale, new RadzenValueAxis(), new RadzenAxisTitle());

            Assert.True(double.IsFinite(size));
            Assert.True(size >= 24);
        }

        [Fact]
        public void AxisMeasurer_XAxis_WithDefaultLogarithmicInput_Terminates()
        {
            var scale = new LogarithmicScale
            {
                Input = new ScaleRange { Start = double.PositiveInfinity, End = double.NegativeInfinity }
            };

            var axis = new RadzenCategoryAxis { LabelAutoRotation = 45 };

            var size = AxisMeasurer.XAxis(scale, axis, new RadzenAxisTitle());

            Assert.True(double.IsFinite(size));
        }

        [Theory]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, 10, true)]
        [InlineData(1, 10, 1, true)]
        [InlineData(0, 10, 10, true)]
        [InlineData(0, 10, 0, false)]
        [InlineData(double.NaN, 10, 1, false)]
        public void AxisMeasurer_YAxis_WithDegenerateTicks_Terminates(double start, double end, double step, bool isLogarithmic)
        {
            // Defense in depth: even if a scale produces ticks that cannot be enumerated
            // (non-finite bounds, multiplicative step <= 1, additive step of zero) measuring
            // the axis must not loop forever.
            var scale = new BrokenScale(start, end, step, isLogarithmic);

            var size = AxisMeasurer.YAxis(scale, new RadzenValueAxis(), new RadzenAxisTitle());

            Assert.True(double.IsFinite(size));
        }

        class BrokenScale : ScaleBase
        {
            private readonly (double Start, double End, double Step) ticks;
            private readonly bool isLogarithmic;

            public BrokenScale(double start, double end, double step, bool logarithmic)
            {
                ticks = (start, end, step);
                isLogarithmic = logarithmic;
            }

            public override bool IsLogarithmic => isLogarithmic;

            public override (double Start, double End, double Step) Ticks(int distance) => ticks;

            public override double Scale(double value, bool padding = false) => value;

            public override object Value(double value) => value;

            public override string FormatTick(string format, object value) => value?.ToString() ?? string.Empty;
        }
    }
}
