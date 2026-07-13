#nullable enable

using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Colour-stop offsets must be honoured: a gradient whose stops do not span the full [0 1]
// parametric domain holds the endpoint colour constant before the first and after the last
// stop, and interpolates each adjacent pair over its own offset sub-range (ISO 32000-1
// 7.10.3 stitching functions).
public class GradientStopOffsetTests
{
    private static DictionaryObject Dict(DocumentObject o) => Assert.IsType<DictionaryObject>(o);

    private static ArrayObject Array(DocumentObject o) => Assert.IsType<ArrayObject>(o);

    private static double Num(DocumentObject o) => Assert.IsType<NumberObject>(o).DoubleValue;

    [Fact]
    public void TwoStops_NonUniformOffsets_StitchWithConstantEnds()
    {
        var brush = new LinearGradient(0, 0, 100, 0,
            new GradientStop(0.3, Color.Red),
            new GradientStop(0.7, Color.Blue));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        Assert.Equal(3, Num(func["FunctionType"]!));

        var functions = Array(func["Functions"]!);
        Assert.Equal(3, functions.Count);

        var bounds = Array(func["Bounds"]!);
        Assert.Equal(2, bounds.Count);
        Assert.Equal(0.3, Num(bounds[0]), 3);
        Assert.Equal(0.7, Num(bounds[1]), 3);

        Assert.Equal(6, Array(func["Encode"]!).Count);

        // Leading segment is constant red (C0 == C1), trailing is constant blue.
        var lead = Dict(functions[0]);
        Assert.Equal(Num(Array(lead["C0"]!)[0]), Num(Array(lead["C1"]!)[0]), 3);
        Assert.Equal(1, Num(Array(lead["C0"]!)[0]), 3); // red channel

        var tail = Dict(functions[2]);
        Assert.Equal(Num(Array(tail["C0"]!)[2]), Num(Array(tail["C1"]!)[2]), 3);
        Assert.Equal(1, Num(Array(tail["C0"]!)[2]), 3); // blue channel

        // Middle segment interpolates red -> blue.
        var mid = Dict(functions[1]);
        Assert.Equal(1, Num(Array(mid["C0"]!)[0]), 3);
        Assert.Equal(1, Num(Array(mid["C1"]!)[2]), 3);
    }

    [Fact]
    public void TwoStops_FullSpan_StayExponentialTypeTwo()
    {
        var brush = new LinearGradient(0, 0, 100, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        Assert.Equal(2, Num(func["FunctionType"]!));
    }

    [Fact]
    public void ThreeStops_TrailingGap_AppendsConstantSegment()
    {
        var brush = new LinearGradient(0, 0, 100, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(0.5, Color.FromRgb(0, 255, 0)),
            new GradientStop(0.8, Color.Blue));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        Assert.Equal(3, Num(func["FunctionType"]!));
        Assert.Equal(3, Array(func["Functions"]!).Count);

        var bounds = Array(func["Bounds"]!);
        Assert.Equal(2, bounds.Count);
        Assert.Equal(0.5, Num(bounds[0]), 3);
        Assert.Equal(0.8, Num(bounds[1]), 3);
    }
}
