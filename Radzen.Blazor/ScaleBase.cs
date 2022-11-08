using System;
using System.Linq;
using System.Linq.Expressions;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class for RadzenChart scales.
    /// </summary>
    public abstract class ScaleBase
    {
        /// <summary>
        /// Gets or sets the input.
        /// </summary>
        /// <value>The input.</value>
        public ScaleRange Input { get; set; } = new ScaleRange();

        /// <summary>
        /// Gets or sets the output.
        /// </summary>
        /// <value>The output.</value>
        public ScaleRange Output { get; set; } = new ScaleRange();

        /// <summary>
        /// Gets the size of the output.
        /// </summary>
        /// <value>The size of the output.</value>
        public double OutputSize
        {
            get
            {
                return Math.Abs(Output.End - Output.Start);
            }
        }

        /// <summary>
        /// Calculates the number of ticks with the specified distance.
        /// </summary>
        /// <param name="distance">The distance.</param>
        abstract public (double Start, double End, double Step) Ticks(int distance);

        /// <summary>
        /// Converts the specified value to a value from this scale with optional padding.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="padding">Whether to apply padding.</param>
        abstract public double Scale(double value, bool padding = false);

        /// <summary>
        /// Composes the specified selector.
        /// </summary>
        /// <typeparam name="TItem">The type of the t item.</typeparam>
        /// <param name="selector">The selector.</param>
        public virtual Func<TItem, double> Compose<TItem>(Func<TItem, double> selector)
        {
            return (value) => Scale(selector(value), true);
        }

        /// <summary>
        /// Gets or sets the step.
        /// </summary>
        /// <value>The step.</value>
        public object Step { get; set; }

        /// <summary>
        /// Resizes the scale to the specified values.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        public virtual void Resize(object min, object max)
        {
            if (min != null)
            {
                Input.Start = Convert.ToDouble(min);
                Round = false;
            }

            if (max != null)
            {
                Input.End = Convert.ToDouble(max);
                Round = false;
            }
        }

        /// <summary>
        /// Returns a "nice" number (closest power of 10).
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="round">Wether to round.</param>
        public double NiceNumber(double range, bool round)
        {
            var sign = Math.Sign(range);
            range = Math.Abs(range);
            var exponent = Math.Floor(Math.Log10(range));
            var fraction = range / Math.Pow(10, exponent);

            double niceFraction;

            if (round)
            {
                if (fraction < 1.5) niceFraction = 1;
                else if (fraction < 3) niceFraction = 2;
                else if (fraction < 7) niceFraction = 5;
                else niceFraction = 10;
            }
            else
            {
                if (fraction <= 1) niceFraction = 1;
                else if (fraction <= 2) niceFraction = 2;
                else if (fraction <= 5) niceFraction = 5;
                else niceFraction = 10;
            }

            return sign * niceFraction * Math.Pow(10, exponent);
        }

        /// <summary>
        /// Gets or sets the padding.
        /// </summary>
        /// <value>The padding.</value>
        public double Padding { get; set; }

        /// <summary>
        /// Fits the scale within the distance.
        /// </summary>
        /// <param name="distance">The distance.</param>
        public virtual void Fit(int distance)
        {
            var ticks = Ticks(distance);

            Input.MergeWidth(new ScaleRange { Start = ticks.Start, End = ticks.End });

            Round = false;
            Step = ticks.Step;
        }

        /// <summary>
        /// Returns a value from the scale.
        /// </summary>
        /// <param name="value">The value.</param>
        public abstract object Value(double value);

        /// <summary>
        /// Formats the tick value.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        public abstract string FormatTick(string format, object value);

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ScaleBase"/> is round.
        /// </summary>
        /// <value><c>true</c> if round; otherwise, <c>false</c>.</value>
        public bool Round { get; set; } = true;

        /// <summary>
        /// Determines whether the specified scale is equal to the current one.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns><c>true</c> if the scales are equal; otherwise, <c>false</c>.</returns>
        public bool IsEqualTo(ScaleBase scale)
        {
            if (GetType() != scale.GetType())
            {
                return false;
            }

            return Input.IsEqualTo(scale.Input) && Output.IsEqualTo(scale.Output);
        }
    }
}