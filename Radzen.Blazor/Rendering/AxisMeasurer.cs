using System;

namespace Radzen.Blazor.Rendering
{
    public static class AxisMeasurer
    {
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