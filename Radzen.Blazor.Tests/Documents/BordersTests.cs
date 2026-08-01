#nullable enable
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;


public class BordersTests
{
    [Fact]
    public void Width_FlowsToAllEdges()
    {
        var borders = new Borders { Width = 1 };
        Assert.Equal(1, borders.Top.Width.Point, 9);
        Assert.Equal(1, borders.Right.Width.Point, 9);
        Assert.Equal(1, borders.Bottom.Width.Point, 9);
        Assert.Equal(1, borders.Left.Width.Point, 9);
    }

    [Fact]
    public void EdgeOverride_LeavesOtherEdgesAtBoxValue()
    {
        var borders = new Borders { Width = 1 };
        borders.Bottom.Width = 1.5;

        Assert.Equal(1, borders.Top.Width.Point, 9);
        Assert.Equal(1, borders.Right.Width.Point, 9);
        Assert.Equal(1, borders.Left.Width.Point, 9);
        Assert.Equal(1.5, borders.Bottom.Width.Point, 9);
    }

    [Fact]
    public void BoxWidthChange_DoesNotOverrideAlreadyOverriddenEdge()
    {
        var borders = new Borders { Width = 1 };
        borders.Bottom.Width = 1.5;
        borders.Width = 2;

        Assert.Equal(2, borders.Top.Width.Point, 9);
        Assert.Equal(2, borders.Right.Width.Point, 9);
        Assert.Equal(2, borders.Left.Width.Point, 9);
        Assert.Equal(1.5, borders.Bottom.Width.Point, 9);
    }

    [Fact]
    public void BoxColorAndStyle_FlowToEdges()
    {
        var borders = new Borders
        {
            Color = Color.Red,
            Style = BorderStyle.Dashed
        };

        Assert.Equal(Color.Red, borders.Top.Color);
        Assert.Equal(BorderStyle.Dashed, borders.Left.Style);
    }
}
