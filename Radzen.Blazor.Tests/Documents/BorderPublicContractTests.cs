#nullable enable
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

public class BorderPublicContractTests
{
    [Fact]
    public void UnsetEdgeDefaultsToZeroBlackAndNone()
    {
        var border = new Borders().Top;

        Assert.Equal(Unit.Parse("0pt"), border.Width);
        Assert.Equal(Color.Black, border.Color);
        Assert.Equal(BorderStyle.None, border.Style);
    }

    [Fact]
    public void WidthIsAUnit()
    {
        var border = new Borders().Top;
        border.Width = Unit.Parse("2.54cm");

        Assert.Equal(Unit.Parse("1in"), border.Width);
    }

    [Fact]
    public void UniformValuesFlowToEveryEdge()
    {
        var borders = new Borders
        {
            Width = Unit.Parse("2pt"),
            Color = Color.Red,
            Style = BorderStyle.Dashed,
        };

        foreach (var edge in new[] { borders.Top, borders.Right, borders.Bottom, borders.Left })
        {
            Assert.Equal(Unit.Parse("2pt"), edge.Width);
            Assert.Equal(Color.Red, edge.Color);
            Assert.Equal(BorderStyle.Dashed, edge.Style);
        }
    }

    [Fact]
    public void PerEdgeValuesOverrideOnlyThatEdge()
    {
        var borders = new Borders
        {
            Width = Unit.Parse("2pt"),
            Color = Color.Red,
            Style = BorderStyle.Dashed,
        };
        borders.Bottom.Width = Unit.Parse("4pt");
        borders.Bottom.Color = Color.Blue;
        borders.Bottom.Style = BorderStyle.Dotted;

        Assert.Equal(Unit.Parse("4pt"), borders.Bottom.Width);
        Assert.Equal(Color.Blue, borders.Bottom.Color);
        Assert.Equal(BorderStyle.Dotted, borders.Bottom.Style);
        Assert.Equal(Unit.Parse("2pt"), borders.Top.Width);
        Assert.Equal(Color.Red, borders.Top.Color);
        Assert.Equal(BorderStyle.Dashed, borders.Top.Style);
    }

    [Fact]
    public void PerEdgeOverridesRemainWhenUniformValuesChange()
    {
        var borders = new Borders
        {
            Width = Unit.Parse("2pt"),
            Color = Color.Red,
            Style = BorderStyle.Dashed,
        };
        borders.Left.Width = Unit.Parse("4pt");
        borders.Left.Color = Color.Blue;
        borders.Left.Style = BorderStyle.Dotted;

        borders.Width = Unit.Parse("6pt");
        borders.Color = Color.Green;
        borders.Style = BorderStyle.Solid;

        Assert.Equal(Unit.Parse("4pt"), borders.Left.Width);
        Assert.Equal(Color.Blue, borders.Left.Color);
        Assert.Equal(BorderStyle.Dotted, borders.Left.Style);
        Assert.Equal(Unit.Parse("6pt"), borders.Right.Width);
        Assert.Equal(Color.Green, borders.Right.Color);
        Assert.Equal(BorderStyle.Solid, borders.Right.Style);
    }

}
