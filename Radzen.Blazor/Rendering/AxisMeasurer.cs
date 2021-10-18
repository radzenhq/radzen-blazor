using System;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class AxisMeasurer.
    /// </summary>
    public static class AxisMeasurer
    {
        /// <summary>
        /// ies the axis.
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
        /// xes the axis.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <returns>System.Double.</returns>
        public static double XAxis(RadzenAxisTitle title)
        {
            var size = 16 * 0.875 + 12;

            if (!String.IsNullOrEmpty(title.Text))
            {
                size += title.Size + 24;
            }

            return size;
        }
    }
}