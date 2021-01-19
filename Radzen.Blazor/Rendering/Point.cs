using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public string Render()
        {
            return $"{X.ToInvariantString()} {Y.ToInvariantString()}";
        }

        public Point ToCartesian(double radius, double angle)
        {
            return Polar.ToCartesian(X, Y, radius, angle);
        }
    }
}
