#nullable enable
using System;
using System.IO;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class OpacityPublicContractTests
{
    [Theory]
    [InlineData(-0.000001)]
    [InlineData(1.000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void EveryOpacitySurfaceRejectsOutOfRangeAndNonFiniteValues(double value)
    {
        var document = new Document();
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(new MemoryStream());
        var run = section.Blocks.AddParagraph().Inlines.Add("text");
        var container = new Container();
        var watermark = new Watermark();

        Assert.Throws<ArgumentOutOfRangeException>(() => image.Opacity = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => run.Opacity = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => container.Opacity = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => watermark.Opacity = value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EveryOpacitySurfaceAcceptsInclusiveBoundaryValues(double value)
    {
        var document = new Document();
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(new MemoryStream());
        var run = section.Blocks.AddParagraph().Inlines.Add("text");
        var container = new Container();
        var watermark = new Watermark();

        image.Opacity = value;
        run.Opacity = value;
        container.Opacity = value;
        watermark.Opacity = value;

        Assert.Equal(value, image.Opacity);
        Assert.Equal(value, run.Opacity);
        Assert.Equal(value, container.Opacity);
        Assert.Equal(value, watermark.Opacity);
    }
}
