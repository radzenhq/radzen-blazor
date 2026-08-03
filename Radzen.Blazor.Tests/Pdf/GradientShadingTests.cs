#nullable enable

using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Write;
using Radzen.Documents;
using Radzen.Blazor.Tests.Isolated;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 8.7.4.5.2, 8.7.4.5.3, 8.7.4.5.5: axial (type 2) and radial (type 3) shadings and the shading Pattern (PatternType 2).
public class GradientShadingTests
{
    private static DictionaryObject Shading(GradientBrush brush)
        => (DictionaryObject)ShadingBuilder.BuildPattern(brush)["Shading"]!;

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

        var shading = Shading(brush);

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

        var shading = Shading(brush);

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

        var func = Dict(Shading(brush)["Function"]!);

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

        var func = Dict(Shading(brush)["Function"]!);

        Assert.Equal(3, Num(func["FunctionType"]!));
        Assert.Equal(2, Array(func["Functions"]!).Count);

        var bounds = Array(func["Bounds"]!);
        Assert.Single(bounds);
        Assert.Equal(0.5, Num(bounds[0]), 3);

        Assert.Equal(4, Array(func["Encode"]!).Count);
    }

    [Fact]
    public void Extend_IsAlwaysBothEnds()
    {
        var brush = new LinearGradient(0, 0, 10, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        var extend = Array(Shading(brush)["Extend"]!);

        Assert.True(Assert.IsType<BooleanObject>(extend[0]).Value);
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
    public void BoxGradient_PatternMatrix_MapsBoxRelativeTopOriginCoordinatesOntoTheBox()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Width = Unit.FromPoint(120),
            BackgroundGradient = new LinearGradient(
                0, 0, 100, 0,
                new GradientStop(0, Color.Red),
                new GradientStop(1, Color.Blue)),
        });
        container.Blocks.AddParagraph().Inlines.Add("Boxed");

        var reader = BuildTestSupport.Read(document);
        var page = Assert.Single(BuildTestSupport.PageLeaves(reader));
        var patterns = reader.GetDictionary(page.Resources!, "Pattern");
        var pattern = Dict(reader.Resolve(patterns![Assert.Single(patterns.Keys)])!);
        var matrix = Array(reader.Resolve(pattern["Matrix"]!)!);

        var box = Assert.Single(IsolatedPaginator.PaginateIsolated(
            section,
            new Radzen.Documents.Fonts.FontCollection())).Body.Boxes[0];
        Assert.Equal(1, Num(matrix[0]), 3);
        Assert.Equal(0, Num(matrix[1]), 3);
        Assert.Equal(0, Num(matrix[2]), 3);
        Assert.Equal(-1, Num(matrix[3]), 3);
        Assert.Equal(section.Margins.Left.Point + box.Bounds.X, Num(matrix[4]), 3);
        Assert.Equal(section.PageSize.Height.Point - section.Margins.Top.Point - box.Bounds.Y, Num(matrix[5]), 3);
    }

    [Fact]
    public void SingleStop_ProducesConstantFunction()
    {
        var brush = new LinearGradient(0, 0, 10, 0, new GradientStop(0, Color.Red));

        var func = Dict(Shading(brush)["Function"]!);

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

        var linearShading = Shading(linear);
        var radialShading = Shading(radial);

        Assert.Equal(2, Num(linearShading["ShadingType"]!));
        Assert.Equal(4, Array(linearShading["Coords"]!).Count);
        Assert.Equal(3, Num(radialShading["ShadingType"]!));
        Assert.Equal(6, Array(radialShading["Coords"]!).Count);
    }
}
