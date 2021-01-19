using System;

namespace Radzen.Blazor.Rendering
{
    public static class Polar
    {
        public static double ToRadian(double value)
        {
            return (Math.PI / 180) * value;
        }

        public static Point ToCartesian(double cx, double cy, double radius, double angle)
        {
            var x = cx + radius * Math.Cos(angle);
            var y = cy + radius * Math.Sin(angle);

            return new Point { X = x, Y = y };
        }
    }
}