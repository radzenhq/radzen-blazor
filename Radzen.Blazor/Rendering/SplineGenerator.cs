using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Blazor.Rendering
{
    public class SplineGenerator : IPathGenerator
    {
        class SplinePoint
        {
            public double X;
            public double Y;
            public double ControlPointPreviousX;
            public double ControlPointPreviousY;
            public double ControlPointNextX;
            public double ControlPointNextY;
        }

        class PointWithTanget
        {
            public SplinePoint Point;
            public double Delta;
            public double MK;
        }

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
