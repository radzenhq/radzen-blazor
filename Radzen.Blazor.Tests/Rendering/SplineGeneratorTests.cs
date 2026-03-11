using System.Collections.Generic;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests.Rendering;

public class SplineGeneratorTests
{
    [Fact]
    public void Renders_Path_For_Single_Point()
    {
        var data = new List<Point>
        {
            new() { X = 10, Y = 10 }
        };

        var path = new SplineGenerator().Path(data);

        Assert.Equal("10 10 ", path);
    }

    [Fact]
    public void Renders_Spline_Path_For_Multiple_Points()
    {
        var data = new List<Point>
        {
            new() { X = 0, Y = 0 },
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = 0 }
        };

        var path = new SplineGenerator().Path(data);

        Assert.StartsWith("0 0 ", path);
        Assert.Contains("C ", path);
    }

    [Fact]
    public void NaN_Y_Creates_Gap_In_Spline()
    {
        var data = new List<Point>
        {
            new() { X = 0, Y = 0 },
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = double.NaN },
            new() { X = 30, Y = 30 },
            new() { X = 40, Y = 40 }
        };

        var path = new SplineGenerator().Path(data);

        // Should contain M to start a new segment after the gap
        Assert.Contains("M ", path);
        // First segment starts at 0,0
        Assert.StartsWith("0 0 ", path);
    }

    [Fact]
    public void NaN_At_Start_Skips_Initial_Point()
    {
        var data = new List<Point>
        {
            new() { X = double.NaN, Y = double.NaN },
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = 20 }
        };

        var path = new SplineGenerator().Path(data);

        Assert.StartsWith("10 10 ", path);
    }

    [Fact]
    public void Multiple_Gaps_Create_Multiple_Segments()
    {
        var data = new List<Point>
        {
            new() { X = 0, Y = 0 },
            new() { X = 10, Y = 10 },
            new() { X = 20, Y = double.NaN },
            new() { X = 30, Y = 30 },
            new() { X = 40, Y = 40 },
            new() { X = 50, Y = double.NaN },
            new() { X = 60, Y = 60 },
            new() { X = 70, Y = 70 }
        };

        var path = new SplineGenerator().Path(data);

        // Count M commands - should have 2 (for 2nd and 3rd segments)
        var mCount = 0;
        var index = 0;
        while ((index = path.IndexOf("M ", index)) != -1)
        {
            mCount++;
            index += 2;
        }
        Assert.Equal(2, mCount);
    }

    [Fact]
    public void All_NaN_Returns_Empty_Path()
    {
        var data = new List<Point>
        {
            new() { X = double.NaN, Y = double.NaN },
            new() { X = double.NaN, Y = double.NaN }
        };

        var path = new SplineGenerator().Path(data);

        Assert.Equal("", path);
    }
}
