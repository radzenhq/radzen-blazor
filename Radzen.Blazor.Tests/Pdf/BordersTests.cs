#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

using Colors = Radzen.Documents.Pdf.Colors;

public class BordersTests
{
    [Fact]
    public void Width_FlowsToAllEdges()
    {
        var borders = new Borders { Width = 1 };
        Assert.Equal(1, borders.Top.Width, 9);
        Assert.Equal(1, borders.Right.Width, 9);
        Assert.Equal(1, borders.Bottom.Width, 9);
        Assert.Equal(1, borders.Left.Width, 9);
    }

    [Fact]
    public void EdgeOverride_LeavesOtherEdgesAtBoxValue()
    {
        var borders = new Borders { Width = 1 };
        borders.Bottom.Width = 1.5;

        Assert.Equal(1, borders.Top.Width, 9);
        Assert.Equal(1, borders.Right.Width, 9);
        Assert.Equal(1, borders.Left.Width, 9);
        Assert.Equal(1.5, borders.Bottom.Width, 9);
    }

    [Fact]
    public void BoxWidthChange_DoesNotOverrideAlreadyOverriddenEdge()
    {
        var borders = new Borders { Width = 1 };
        borders.Bottom.Width = 1.5;
        borders.Width = 2;

        Assert.Equal(2, borders.Top.Width, 9);
        Assert.Equal(2, borders.Right.Width, 9);
        Assert.Equal(2, borders.Left.Width, 9);
        Assert.Equal(1.5, borders.Bottom.Width, 9);
    }

    [Fact]
    public void BoxColorAndStyle_FlowToEdges()
    {
        var borders = new Borders
        {
            Color = Colors.Red,
            Style = BorderStyle.Dashed
        };

        Assert.Equal(Colors.Red, borders.Top.Color);
        Assert.Equal(BorderStyle.Dashed, borders.Left.Style);
    }

    [Fact]
    public void Border_PropertiesAreSettable()
    {
        var border = new Border
        {
            Width = 3,
            Color = Colors.Blue,
            Style = BorderStyle.Dotted
        };

        Assert.Equal(3, border.Width, 9);
        Assert.Equal(Colors.Blue, border.Color);
        Assert.Equal(BorderStyle.Dotted, border.Style);
    }
}
