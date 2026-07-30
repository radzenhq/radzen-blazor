#nullable enable
using System;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents.Geometry;
using Radzen.Documents.Layout;

namespace Radzen.Blazor.Pdf.Tests;

public class MeasurementUnitSurfaceTests
{
    private static GradientStop[] Stops() =>
    [
        new GradientStop(0, Color.Red),
        new GradientStop(1, Color.Blue),
    ];

    private static GradientPaint Resolve(GradientBrush brush, double width, double height)
        => GeometryCapture.Gradient(brush, GradientReference.Box(width, height))!.Value;

    [Fact]
    public void FontSize_FromMeasurementString_IsPoints()
    {
        var font = new Font { Size = "12pt" };

        Assert.Equal(12, font.Size!.Value.Point);
        Assert.Equal(new Font { Size = 12 }.Size, font.Size);
    }

    [Fact]
    public void FontSize_FromInches_ConvertsToPoints()
    {
        Assert.Equal(72, new Font { Size = Unit.FromInch(1) }.Size!.Value.Point);
    }

    [Fact]
    public void FontSize_DefaultsToUnsetAndResolvesToTenPoints()
    {
        Assert.Null(new Font().Size);

        var document = new Document();
        var paragraph = document.Sections.Add().Blocks.Add(new Paragraph { Text = "x" });

        Assert.Equal(10, document.Resolve(paragraph).Font.Size.Point);
    }

    [Fact]
    public void BorderWidth_FromMeasurementString_IsPoints()
    {
        var borders = new Borders { Width = "1.5pt" };

        Assert.Equal(1.5, borders.Width.Point);
        Assert.Equal(1.5, borders.Top.Width.Point);
    }

    [Fact]
    public void BorderWidth_EdgeOverrideAcceptsMillimeters()
    {
        var borders = new Borders { Width = 1 };
        borders.Left.Width = "1mm";

        Assert.Equal(Unit.FromMillimeter(1).Point, borders.Left.Width.Point);
        Assert.Equal(1, borders.Right.Width.Point);
    }

    [Fact]
    public void LinearGradient_AbsoluteCoordinates_IgnoreBoxExtent()
    {
        var brush = new LinearGradient(0, 0, 100, 20, Stops());

        var paint = Resolve(brush, 400, 50);

        Assert.Equal(0, paint.X0);
        Assert.Equal(0, paint.Y0);
        Assert.Equal(100, paint.X1);
        Assert.Equal(20, paint.Y1);
    }

    [Fact]
    public void LinearGradient_RelativeCoordinates_AreFractionsOfTheBox()
    {
        var brush = new LinearGradient(
            Unit.FromPercent(35), Unit.FromPercent(35),
            Unit.FromPercent(100), Unit.FromPercent(50),
            Stops());

        var paint = Resolve(brush, 400, 50);

        Assert.Equal(0.35 * 400, paint.X0);
        Assert.Equal(0.35 * 50, paint.Y0);
        Assert.Equal(1.0 * 400, paint.X1);
        Assert.Equal(0.5 * 50, paint.Y1);
    }

    [Fact]
    public void LinearGradient_MixesRelativeAndAbsoluteCoordinates()
    {
        var brush = new LinearGradient(Unit.FromPercent(50), 4, "1in", Unit.FromPercent(25), Stops());

        var paint = Resolve(brush, 200, 80);

        Assert.Equal(0.5 * 200, paint.X0);
        Assert.Equal(4, paint.Y0);
        Assert.Equal(72, paint.X1);
        Assert.Equal(0.25 * 80, paint.Y1);
    }

    [Fact]
    public void RadialGradient_RelativeRadiusIsAFractionOfTheBoxWidth()
    {
        var brush = new RadialGradient(
            Unit.FromPercent(50), Unit.FromPercent(50), 0,
            Unit.FromPercent(50), Unit.FromPercent(50), Unit.FromPercent(40),
            Stops());

        var paint = Resolve(brush, 120, 60);

        Assert.Equal(0.5 * 120, paint.X0);
        Assert.Equal(0.5 * 60, paint.Y0);
        Assert.Equal(0, paint.R0);
        Assert.Equal(0.4 * 120, paint.R1);
    }

    [Fact]
    public void RelativeGradient_WithoutReferenceBox_Throws()
    {
        var brush = new LinearGradient(Unit.FromPercent(0), 0, Unit.FromPercent(100), 0, Stops());

        Assert.Throws<InvalidOperationException>(() => ShadingBuilder.BuildShading(brush));
    }

    [Fact]
    public void RelativeGradient_ResolvesAgainstTheContainerBox()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Width = Unit.FromPoint(200),
            BackgroundGradient = new LinearGradient(
                Unit.FromPercent(0), 0, Unit.FromPercent(100), 0, Stops()),
        });
        container.Blocks.Add(FeatureEmissionTestHelpers.Text("Boxed"));

        var reader = BuildTestSupport.Read(document);
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var patterns = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Pattern"]!));
        var pattern = Assert.IsType<DictionaryObject>(reader.Resolve(patterns[Assert.Single(patterns.Keys)]));
        var shading = Assert.IsType<DictionaryObject>(reader.Resolve(pattern["Shading"]!));
        var coords = Assert.IsType<ArrayObject>(reader.Resolve(shading["Coords"]!));

        Assert.Equal(0, Assert.IsType<NumberObject>(coords[0]).DoubleValue);
        Assert.Equal(200, Assert.IsType<NumberObject>(coords[2]).DoubleValue);
    }
}
