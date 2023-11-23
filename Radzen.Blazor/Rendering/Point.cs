using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class Point.
    /// </summary>
    public class Point
    {
        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        public double X { get; set; }
        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        public double Y { get; set; }

        /// <summary>
        /// Renders this instance.
        /// </summary>
        /// <returns>System.String.</returns>
        public string Render()
        {
            return $"{X.ToInvariantString()} {Y.ToInvariantString()}";
        }

        /// <summary>
        /// Converts to cartesian.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="angle">The angle.</param>
        /// <returns>Point.</returns>
        public Point ToCartesian(double radius, double angle)
        {
            return Polar.ToCartesian(X, Y, radius, angle);
        }
    }

    /// <summary>
    /// Represents a point with data.
    /// </summary>
    public class Point<T> : Point
    {
        /// <summary>
        /// The data associated with the point.
        /// </summary>
        public T Data { get; set; }
    }
}
