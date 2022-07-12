using System;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class Polar.
    /// </summary>
    public static class Polar
    {
        /// <summary>
        /// Converts to radian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Double.</returns>
        public static double ToRadian(double value)
        {
            return (Math.PI / 180) * value;
        }

        /// <summary>
        /// Converts to degrees.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Double.</returns>
        public static double ToDegree(double value)
        {
            return (180 / Math.PI) * value;
        }

        /// <summary>
        /// Converts to cartesian.
        /// </summary>
        /// <param name="cx">The cx.</param>
        /// <param name="cy">The cy.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="angle">The angle.</param>
        /// <returns>Point.</returns>
        public static Point ToCartesian(double cx, double cy, double radius, double angle)
        {
            var x = cx + radius * Math.Cos(angle);
            var y = cy + radius * Math.Sin(angle);

            return new Point { X = x, Y = y };
        }
    }
}