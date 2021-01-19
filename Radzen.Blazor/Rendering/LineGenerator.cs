using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Blazor.Rendering
{
    public class LineGenerator : IPathGenerator
    {
        public string Path(IEnumerable<Point> data)
        {
            var path = new StringBuilder();
            var start = true;

            foreach (var item in data)
            {
                var x = item.X;
                var y = item.Y;

                if (start)
                {
                    start = false;
                }
                else
                {
                    path.Append("L ");
                }

                path.Append($"{x.ToInvariantString()} {y.ToInvariantString()} ");
            }

            return path.ToString();
        }
    }
}
