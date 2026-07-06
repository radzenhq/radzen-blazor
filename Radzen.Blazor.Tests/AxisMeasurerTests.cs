using Radzen.Blazor.Rendering;
using System;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AxisMeasurerTests
    {
        private class TestAxis : AxisBase
        {
            internal override double Size => 0;
        }

        private class TestScale : ScaleBase
        {
            private readonly (double Start, double End, double Step) _ticks;
            private readonly bool _isLog;
            private readonly Func<double, string> _formatter;

            public TestScale((double Start, double End, double Step) ticks, bool isLog = false, Func<double, string>? formatter = null)
            {
                _ticks = ticks;
                _isLog = isLog;
                _formatter = formatter ?? (v => v.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
                // Provide valid Input/Output for some Scale implementations if needed
                Input.Start = ticks.Start;
                Input.End = ticks.End;
                Output.Start = 0;
                Output.End = 100;
            }

            public override object Value(double value) => value;

            public override string FormatTick(string format, object value)
            {
                if (value is double d) return _formatter(d);
                return value?.ToString() ?? string.Empty;
            }

            public override (double Start, double End, double Step) Ticks(int distance) => _ticks;

            public override double Scale(double value, bool padding = false) => value;

            public override bool IsLogarithmic => _isLog;
        }

        [Fact]
        public void YAxis_Returns_AxisWidth_When_Provided_And_Valid()
        {
            var scale = new TestScale((0, 0, 1));
            var axis = new TestAxis { Width = 50 };
            var title = new RadzenAxisTitle { Text = null };

            var result = AxisMeasurer.YAxis(scale, axis, title);

            Assert.Equal(50d, result);
        }

        [Fact]
        public void YAxis_Throws_When_Width_Less_Than_Minimum()
        {
            var scale = new TestScale((0, 0, 1));
            var axis = new TestAxis { Width = 10 };
            var title = new RadzenAxisTitle { Text = null };

            Assert.Throws<ArgumentOutOfRangeException>(() => AxisMeasurer.YAxis(scale, axis, title));
        }
    }
}