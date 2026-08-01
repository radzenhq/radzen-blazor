#nullable enable

using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Write;
using Radzen.Documents;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.10.3 (stitching functions): each adjacent stop pair interpolates over its own offset sub-range; endpoints stay constant outside [first, last].
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

        var lead = Dict(functions[0]);
        Assert.Equal(Num(Array(lead["C0"]!)[0]), Num(Array(lead["C1"]!)[0]), 3);
        Assert.Equal(1, Num(Array(lead["C0"]!)[0]), 3);

        var tail = Dict(functions[2]);
        Assert.Equal(Num(Array(tail["C0"]!)[2]), Num(Array(tail["C1"]!)[2]), 3);
        Assert.Equal(1, Num(Array(tail["C0"]!)[2]), 3);

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
    public void HardStopAtDomainEnd_KeepsBoundsWithinDomain()
    {
        var brush = new LinearGradient(0, 0, 100, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(0.5, Color.Green),
            new GradientStop(1, Color.Green),
            new GradientStop(1, Color.Blue));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        Assert.Equal(3, Num(func["FunctionType"]!));
        var bounds = Array(func["Bounds"]!);
        Assert.Equal(Array(func["Functions"]!).Count - 1, bounds.Count);
        foreach (var bound in bounds)
        {
            var value = Num(bound);
            Assert.True(value > 0 && value < 1, $"Bounds entry {value} is outside the open domain (0, 1).");
        }
    }

    [Fact]
    public void HardStopAtDomainStart_KeepsBoundsWithinDomain()
    {
        var brush = new LinearGradient(0, 0, 100, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(0, Color.Green),
            new GradientStop(0.5, Color.Green),
            new GradientStop(1, Color.Blue));

        var func = Dict(ShadingBuilder.BuildShading(brush)["Function"]!);

        var bounds = Array(func["Bounds"]!);
        Assert.Equal(Array(func["Functions"]!).Count - 1, bounds.Count);
        foreach (var bound in bounds)
        {
            var value = Num(bound);
            Assert.True(value > 0 && value < 1, $"Bounds entry {value} is outside the open domain (0, 1).");
        }
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
