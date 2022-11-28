using System.Collections.Generic;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests.Rendering;

public class StepGeneratorTests
{
    [Fact]
    public void Renders_Path_Correctly()
    {
        var data = new List<Point>
        {
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = 15 },
            new() { X = 30, Y = 20 },
            new() { X = 40, Y = 25 },
            new() { X = 50, Y = 50 }
        };

        var path = new StepGenerator().Path(data);
        
        Assert.Equal("10 10 H 20 V 15 H 30 V 20 H 40 V 25 H 50 V 50", path);
    }
}
