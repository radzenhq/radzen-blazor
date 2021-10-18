using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class SplineGenerator.
    /// Implements the <see cref="Radzen.Blazor.Rendering.IPathGenerator" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.Rendering.IPathGenerator" />
    public class SplineGenerator : IPathGenerator
    {
        /// <summary>
        /// Class SplinePoint.
        /// </summary>
        class SplinePoint
        {
            /// <summary>
            /// The x
            /// </summary>
            public double X;
            /// <summary>
            /// The y
            /// </summary>
            public double Y;
            /// <summary>
            /// The control point previous x
            /// </summary>
            public double ControlPointPreviousX;
            /// <summary>
            /// The control point previous y
            /// </summary>
            public double ControlPointPreviousY;
            /// <summary>
            /// The control point next x
            /// </summary>
            public double ControlPointNextX;
            /// <summary>
            /// The control point next y
            /// </summary>
            public double ControlPointNextY;
        }

        /// <summary>
        /// Class PointWithTanget.
        /// </summary>
        class PointWithTanget
        {
            /// <summary>
            /// The point
            /// </summary>
            public SplinePoint Point;
            /// <summary>
            /// The delta
            /// </summary>
            public double Delta;
            /// <summary>
            /// The mk
            /// </summary>
            public double MK;
        }

        /// <summary>
        /// Curves the monotone.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>IList&lt;PointWithTanget&gt;.</returns>
        IList<PointWithTanget> CurveMonotone(IList<SplinePoint> points)
        {
            var pointsWithTangents = points.Select(point => new PointWithTanget { Point = point, Delta = 0, MK = 0 }).ToList();

            var pointCount = pointsWithTangents.Count();

            for (var i = 0; i < pointCount; i++)
            {
                var currentPoint = pointsWithTangents[i];

                var previousPoint = i > 0 ? pointsWithTangents[i - 1] : null;
                var nextPoint = i < pointCount - 1 ? pointsWithTangents[i + 1] : null;

                if (nextPoint != null)
                {
                    var slopeDeltaX = (nextPoint.Point.X - currentPoint.Point.X);

                    currentPoint.Delta = slopeDeltaX != 0 ? (nextPoint.Point.Y - currentPoint.Point.Y) / slopeDeltaX : 0;
                }

                if (previousPoint == null)
                {
                    currentPoint.MK = currentPoint.Delta;
                }
                else if (nextPoint == null)
                {
                    currentPoint.MK = previousPoint.Delta;
                }
                else if (Math.Sign(previousPoint.Delta) != Math.Sign(currentPoint.Delta))
                {
                    currentPoint.MK = 0;
                }
                else
                {
                    currentPoint.MK = (previousPoint.Delta + currentPoint.Delta) / 2;
                }
            }

            for (var i = 0; i < pointCount - 1; i++)
            {
                var currentPoint = pointsWithTangents[i];
                var nextPoint = pointsWithTangents[i + 1];

                if (currentPoint.Delta == 0)
                {
                    currentPoint.MK = nextPoint.MK = 0;
                    continue;
                }

                var alphaK = currentPoint.MK / currentPoint.Delta;
                var betaK = nextPoint.MK / currentPoint.Delta;
                var squaredMagnitude = Math.Pow(alphaK, 2) + Math.Pow(betaK, 2);

                if (squaredMagnitude <= 9)
                {
                    continue;
                }

                var tauK = 3 / Math.Sqrt(squaredMagnitude);
                currentPoint.MK = alphaK * tauK * currentPoint.Delta;
                nextPoint.MK = betaK * tauK * currentPoint.Delta;
            }

            for (var i = 0; i < pointCount; i++)
            {
                var currentPoint = pointsWithTangents[i];

                var previousPoint = i > 0 ? pointsWithTangents[i - 1] : null;
                var nextPoint = i < pointCount - 1 ? pointsWithTangents[i + 1] : null;

                if (previousPoint != null)
                {
                    var deltaX = (currentPoint.Point.X - previousPoint.Point.X) / 3;
                    currentPoint.Point.ControlPointPreviousX = currentPoint.Point.X - deltaX;
                    currentPoint.Point.ControlPointPreviousY = currentPoint.Point.Y - deltaX * currentPoint.MK;
                }

                if (nextPoint != null)
                {
                    var deltaX = (nextPoint.Point.X - currentPoint.Point.X) / 3;
                    currentPoint.Point.ControlPointNextX = currentPoint.Point.X + deltaX;
                    currentPoint.Point.ControlPointNextY = currentPoint.Point.Y + deltaX * currentPoint.MK;
                }
            }

            return pointsWithTangents;
        }

        /// <summary>
        /// Pathes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>System.String.</returns>
        public string Path(IEnumerable<Point> data)
        {
            var path = new StringBuilder();

            var points = data.Select(item => new SplinePoint { X = item.X, Y = item.Y }).ToList();

            path.Append($"{points[0].X.ToInvariantString()} {points[0].Y.ToInvariantString()} ");

            var pointsWithTangents = CurveMonotone(points);
            var count = pointsWithTangents.Count();

            for (var i = 1; i < count; i++)
            {
                var prev = pointsWithTangents[i - 1].Point;
                var point = pointsWithTangents[i].Point;

                path.Append($"C {prev.ControlPointNextX.ToInvariantString()}, {prev.ControlPointNextY.ToInvariantString()} {point.ControlPointPreviousX.ToInvariantString()}, {point.ControlPointPreviousY.ToInvariantString()} {point.X.ToInvariantString()}, {point.Y.ToInvariantString()}");
            }

            return path.ToString();
        }
    }
}
