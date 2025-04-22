using System;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Measures the sises of the chart axes.
    /// </summary>
    public static class AxisMeasurer
    {
        /// <summary>
        /// Calculates the length of the Y axis.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="title">The title.</param>
        /// <returns>System.Double.</returns>
        public static double YAxis(ScaleBase scale, AxisBase axis, RadzenAxisTitle title)
        {
            var ticks = scale.Ticks(axis.TickDistance);

            double length = 0;

            for (var y = ticks.Start; y <= ticks.End; y += ticks.Step)
            {
                var text = axis.Format(scale, y);

                length = Math.Max(length, TextMeasurer.TextWidth(text));
            }

            if (!String.IsNullOrEmpty(title.Text))
            {
                length += title.Size + 32;
            }

            length += 9 + axis.StrokeWidth;

            return Math.Max(24, length);
        }

        /// <summary>
        /// Calculates the length of the X axis.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="title">The title.</param>
        /// <returns>System.Double.</returns>
        public static double XAxis(ScaleBase scale, AxisBase axis, RadzenAxisTitle title)
        {
            var size = 16 * 0.875 + 12;

            var angle = axis.LabelRotation ?? axis.LabelAutoRotation;

            if (angle != null)
            {
                var ticks = scale.Ticks(axis.TickDistance);

                double length = 0;

                for (var y = ticks.Start; y <= ticks.End; y += ticks.Step)
                {
                    var text = axis.Format(scale, y);

                    length = Math.Max(length, TextMeasurer.TextWidth(text));
                }

                var alpha = Math.Abs(angle.Value) * Math.PI / 180;
                var rotatedWidth = Math.Abs(Math.Sin(alpha) * length) + Math.Abs(Math.Cos(alpha) * size);
                size = Math.Max(size, rotatedWidth);
            }

            if (!String.IsNullOrEmpty(title.Text))
            {
                size += title.Size + 24;
            }

            return size;
        }
    }
}