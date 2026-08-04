#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class BezierGeometryTests
{
    private static string Painted(Action<PathContent> build)
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        var path = new PathContent { Stroke = true };
        build(path);
        page.Content.Add(path);

        var emission = Emit(document);
        var contents = Shaped("page", @"/Contents (\d+) 0 R", Line(emission, "/Type /Page "));
        return IndirectObject(emission, contents.Groups[1].Value);
    }

    [Fact]
    public void Kappa_IsTheQuarterArcConstantToFullDoublePrecision()
    {
        Assert.Equal(4.0 / 3.0 * (Math.Sqrt(2) - 1), BezierGeometry.Kappa, 1e-15);
    }

    [Fact]
    public void AppendCircle_IsAppendEllipseWithEqualRadii()
    {
        var circle = Painted(path => BezierGeometry.AppendCircle(path, 100, 120, 40));
        var ellipse = Painted(path => BezierGeometry.AppendEllipse(path, 100, 120, 40, 40));
        Assert.Equal(ellipse, circle);
    }

    [Theory]
    [InlineData(50.0, 50.0)]
    [InlineData(80.0, 20.0)]
    public void AppendEllipse_MidArcPointsLieOnTheEllipse(double rx, double ry)
    {
        var k = BezierGeometry.Kappa;

        var x = (rx + 3 * rx + 3 * rx * k + 0) / 8.0;
        var y = (0 + 3 * ry * k + 3 * ry + ry) / 8.0;

        var radial = x * x / (rx * rx) + y * y / (ry * ry);
        Assert.InRange(radial, 0.999, 1.001);
    }
}
