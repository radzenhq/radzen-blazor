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

    [Fact]
    public void NaN_Y_Creates_Gap_In_Path()
    {
        var data = new List<Point>
        {
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = double.NaN },
            new() { X = 30, Y = 30 }
        };

        var path = new StepGenerator().Path(data);

        Assert.Equal("10 10 M 30 30", path);
    }

    [Fact]
    public void Multiple_Consecutive_NaN_Creates_Single_Gap()
    {
        var data = new List<Point>
        {
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = double.NaN },
            new() { X = 30, Y = double.NaN },
            new() { X = 40, Y = 40 }
        };

        var path = new StepGenerator().Path(data);

        Assert.Equal("10 10 M 40 40", path);
    }

    [Fact]
    public void Multiple_Gaps_Create_Multiple_Segments()
    {
        var data = new List<Point>
        {
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = 20 },
            new() { X = 30, Y = double.NaN },
            new() { X = 40, Y = 40 },
            new() { X = 50, Y = double.NaN },
            new() { X = 60, Y = 60 }
        };

        var path = new StepGenerator().Path(data);

        Assert.Equal("10 10 H 20 V 20 M 40 40 M 60 60", path);
    }

    [Fact]
    public void NaN_At_Start_Skips_Initial_Point()
    {
        var data = new List<Point>
        {
            new() { X = double.NaN, Y = double.NaN },
            new() { X = 20, Y = 20 },
            new() { X = 30, Y = 30 }
        };

        var path = new StepGenerator().Path(data);

        Assert.Equal("20 20 H 30 V 30", path);
    }
}
