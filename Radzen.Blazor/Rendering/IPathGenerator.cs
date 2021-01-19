using System.Collections.Generic;

namespace Radzen.Blazor.Rendering
{
    public interface IPathGenerator
    {
        string Path(IEnumerable<Point> data);
    }
}
