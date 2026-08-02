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
    public void EdgeValuesAreIndependentOfTheOtherEdges()
    {
        var borders = new Borders();
        borders.SetAll(Unit.Parse("2pt"), Color.Red, BorderStyle.Dashed);

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
}
