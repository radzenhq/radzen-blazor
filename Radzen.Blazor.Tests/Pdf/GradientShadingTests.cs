#nullable enable

using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.7.4.5.2, 8.7.4.5.3, 8.7.4.5.5: axial (type 2) and radial (type 3) shadings and the shading Pattern (PatternType 2).
public class GradientShadingTests
{
    private static DictionaryObject Dict(DocumentObject o) => Assert.IsType<DictionaryObject>(o);

    private static ArrayObject Array(DocumentObject o) => Assert.IsType<ArrayObject>(o);

    private static double Num(DocumentObject o) => Assert.IsType<NumberObject>(o).DoubleValue;

    private static string Name(DocumentObject o) => Assert.IsType<NameObject>(o).Value;

    [Fact]
    public void LinearGradient_BuildsAxialShading()
    {
        var brush = new LinearGradient(0, 0, 100, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        var shading = ShadingBuilder.BuildShading(brush);

        Assert.Equal(2, Num(shading["ShadingType"]!));
        Assert.Equal("DeviceRGB", Name(shading["ColorSpace"]!));

        var coords = Array(shading["Coords"]!);
        Assert.Equal(4, coords.Count);
        Assert.Equal(0, Num(coords[0]), 3);
        Assert.Equal(0, Num(coords[1]), 3);
        Assert.Equal(100, Num(coords[2]), 3);
        Assert.Equal(0, Num(coords[3]), 3);
    }

    [Fact]
    public void RadialGradient_BuildsRadialShadingWithSixCoords()
    {
        var brush = new RadialGradient(50, 50, 0, 50, 50, 40,
            new GradientStop(0, Color.White),
            new GradientStop(1, Color.Black));

        var shading = ShadingBuilder.BuildShading(brush);

        Assert.Equal(3, Num(shading["ShadingType"]!));
        var coords = Array(shading["Coords"]!);
        Assert.Equal(6, coords.Count);
        Assert.Equal(40, Num(coords[5]), 3);
    }

    [Fact]
    public void TwoStops_ProduceExponentialFunctionType2()
    {
        var brush = new LinearGradient(0, 0, 10, 0,
            new GradientStop(0, Color.FromRgb(255, 0, 0)),
            new GradientStop(1, Color.FromRgb(0, 0, 255)));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        Assert.Equal(2, Num(func["FunctionType"]!));
        Assert.Equal(1, Num(func["N"]!));
        var c0 = Array(func["C0"]!);
        var c1 = Array(func["C1"]!);
        Assert.Equal(1, Num(c0[0]), 3);
        Assert.Equal(0, Num(c0[1]), 3);
        Assert.Equal(1, Num(c1[2]), 3);
    }

    [Fact]
    public void ThreeStops_ProduceStitchingFunctionType3()
    {
        var brush = new LinearGradient(0, 0, 30, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(0.5, Color.Green),
            new GradientStop(1, Color.Blue));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        Assert.Equal(3, Num(func["FunctionType"]!));
        Assert.Equal(2, Array(func["Functions"]!).Count);

        var bounds = Array(func["Bounds"]!);
        Assert.Single(bounds);
        Assert.Equal(0.5, Num(bounds[0]), 3);

        Assert.Equal(4, Array(func["Encode"]!).Count);
    }

    [Fact]
    public void Extend_DefaultsToBothTrue_AndHonorsOverrides()
    {
        var brush = new LinearGradient(0, 0, 10, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue))
        {
            ExtendStart = false,
        };

        var extend = Array(ShadingBuilder.BuildShading(brush)["Extend"]!);

        Assert.False(Assert.IsType<BooleanObject>(extend[0]).Value);
        Assert.True(Assert.IsType<BooleanObject>(extend[1]).Value);
    }

    [Fact]
    public void BuildPattern_WrapsShadingAsPatternType2()
    {
        var brush = new LinearGradient(0, 0, 10, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        var pattern = ShadingBuilder.BuildPattern(brush);

        Assert.Equal("Pattern", Name(pattern["Type"]!));
        Assert.Equal(2, Num(pattern["PatternType"]!));
        var shading = Dict(pattern["Shading"]!);
        Assert.Equal(2, Num(shading["ShadingType"]!));
    }

    [Fact]
    public void SingleStop_ProducesConstantFunction()
    {
        var brush = new LinearGradient(0, 0, 10, 0, new GradientStop(0, Color.Red));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        Assert.Equal(2, Num(func["FunctionType"]!));
        var c0 = Array(func["C0"]!);
        var c1 = Array(func["C1"]!);
        Assert.Equal(Num(c0[0]), Num(c1[0]), 3);
    }

    [Fact]
    public void ShadingType_AndCoords_DispatchOnBrushKind()
    {
        GradientBrush linear = new LinearGradient(1, 2, 3, 4,
            new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue));
        GradientBrush radial = new RadialGradient(5, 6, 7, 8, 9, 10,
            new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue));

        Assert.Equal(2, linear.ShadingType);
        Assert.Equal(4, linear.BuildCoords().Count);
        Assert.Equal(3, radial.ShadingType);
        Assert.Equal(6, radial.BuildCoords().Count);
    }
}
