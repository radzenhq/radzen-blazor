using System;
using System.Globalization;

namespace Radzen.Blazor
{
    /// <summary>
    /// A logarithmic scale for chart axes.
    /// All values (Input, Scale, Ticks) are in real-value space (1, 10, 100, 1000).
    /// Scale() converts real values to log-space internally for pixel mapping.
    /// Ticks use multiplicative stepping (step is the multiplication factor, typically the base).
    /// </summary>
    internal class LogarithmicScale : ScaleBase
    {
        public double Base { get; set; } = 10;

        public override bool IsLogarithmic => true;

        private double LogBase => Math.Log(Base);

        public override object Value(double value)
        {
            return value;
        }

        public override string FormatTick(string format, object value)
        {
            if (value == null)
            {
                return String.Empty;
            }

            if (value is double d && d == 0)
            {
                value = 0.0;
            }

            if (String.IsNullOrEmpty(format))
            {
                return value.ToString() ?? String.Empty;
            }

            return string.Format(CultureInfo.InvariantCulture, format, value);
        }

        /// <summary>
        /// Maps a real value to pixel position using logarithmic transformation.
        /// </summary>
        public override double Scale(double value, bool padding = false)
        {
            var outputSize = Output.End - Output.Start;

            if (padding)
            {
                outputSize -= Padding * 2;
            }

            var logMin = Input.Start > 0 ? Math.Log(Input.Start) / LogBase : 0;
            var logMax = Input.End > 0 ? Math.Log(Input.End) / LogBase : 0;
            var logValue = value > 0 ? Math.Log(value) / LogBase : logMin;

            var inputSize = logMax - logMin;
            var result = inputSize != 0 ? (logValue - logMin) / inputSize * outputSize : 0;

            if (outputSize < 0)
            {
                result -= outputSize;
            }

            if (padding)
            {
                result += Padding;
            }

            return result;
        }

        /// <summary>
        /// Returns ticks as real values with multiplicative step.
        /// For base 10 and data range 1-10000: start=1, end=10000, step=10.
        /// The rendering loop should use idx *= step instead of idx += step.
        /// </summary>
        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var start = Input.Start;
            var end = Input.End;

            if (start <= 0)
            {
                start = 1;
            }

            if (end <= start)
            {
                end = start * Base;
            }

            // Round to powers of base
            var logMin = Math.Floor(Math.Log(start) / LogBase);
            var logMax = Math.Ceiling(Math.Log(end) / LogBase);

            start = Math.Pow(Base, logMin);
            end = Math.Pow(Base, logMax);

            // Step is the multiplicative factor
            var step = Base;

            return (start, end, step);
        }

        public override void Fit(int distance)
        {
            var ticks = Ticks(distance);

            Input.Start = ticks.Start;
            Input.End = ticks.End;

            Round = false;
        }
    }
}
