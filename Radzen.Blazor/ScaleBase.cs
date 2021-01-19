using System;
using System.Linq;
using System.Linq.Expressions;

namespace Radzen.Blazor
{
    public abstract class ScaleBase
    {
        public ScaleRange Input { get; set; } = new ScaleRange();

        public ScaleRange Output { get; set; } = new ScaleRange();

        public double OutputSize
        {
            get
            {
                return Math.Abs(Output.End - Output.Start);
            }
        }

        abstract public (double Start, double End, double Step) Ticks(int distance);

        abstract public double Scale(double value, bool padding = false);

        public virtual Func<TItem, double> Compose<TItem>(Func<TItem, double> selector)
        {
            return (value) => Scale(selector(value), true);
        }

        public object Step { get; set; }

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

        public double NiceNumber(double range, bool round)
        {
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

            return niceFraction * Math.Pow(10, exponent);
        }

        public double Padding { get; set; }

        public virtual void Fit(int distance)
        {
            var ticks = Ticks(distance);

            Input.MergeWidth(new ScaleRange { Start = ticks.Start, End = ticks.End });

            Round = false;
            Step = ticks.Step;
        }

        public abstract object Value(double value);

        public abstract string FormatTick(string format, object value);

        public bool Round { get; set; } = true;

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