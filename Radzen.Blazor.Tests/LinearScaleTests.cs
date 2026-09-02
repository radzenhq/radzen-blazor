using System;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LinearScaleTests
    {
        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NiceNumber_WithNonFiniteRange_ReturnsOne(double range)
        {
            var scale = new LinearScale();

            Assert.Equal(1, scale.NiceNumber(range, false));
            Assert.Equal(1, scale.NiceNumber(range, true));
        }

        [Theory]
        // Neither measured nor fed data, so both ranges keep the infinite ScaleRange defaults.
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        // Fed data, but the output has no size yet.
        [InlineData(10, 100, 0, 0)]
        public void Ticks_WithUnmeasuredScale_ReturnsFiniteRange(double inputStart, double inputEnd, double outputStart, double outputEnd)
        {
            var scale = new LinearScale
            {
                Input = new ScaleRange { Start = inputStart, End = inputEnd },
                Output = new ScaleRange { Start = outputStart, End = outputEnd }
            };

            var (start, end, step) = scale.Ticks(0);

            Assert.True(double.IsFinite(start));
            Assert.True(double.IsFinite(end));
            Assert.True(double.IsFinite(step));
            Assert.True(step > 0);
        }

        [Fact]
        public void Ticks_WithMeasuredScale_IsUnaffected()
        {
            var scale = new LinearScale
            {
                Input = new ScaleRange { Start = 0, End = 90 },
                Output = new ScaleRange { Start = 0, End = 500 }
            };

            var (start, end, step) = scale.Ticks(100);

            Assert.Equal(0, start);
            Assert.Equal(100, end);
            Assert.Equal(20, step);
        }
    }
}
