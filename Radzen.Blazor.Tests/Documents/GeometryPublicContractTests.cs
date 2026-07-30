#nullable enable
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class GeometryPublicContractTests
{
    [Fact]
    public void MarginsSetAllAssignsEveryEdge()
    {
        var margins = new Margins();

        margins.SetAll("18pt");

        Assert.Equal(Unit.Parse("18pt"), margins.Top);
        Assert.Equal(Unit.Parse("18pt"), margins.Right);
        Assert.Equal(Unit.Parse("18pt"), margins.Bottom);
        Assert.Equal(Unit.Parse("18pt"), margins.Left);
    }

    [Fact]
    public void MarginEdgesRemainIndependentlyAssignable()
    {
        var margins = new Margins
        {
            Top = "1pt",
            Right = "2pt",
            Bottom = "3pt",
            Left = "4pt",
        };

        Assert.Equal(Unit.Parse("1pt"), margins.Top);
        Assert.Equal(Unit.Parse("2pt"), margins.Right);
        Assert.Equal(Unit.Parse("3pt"), margins.Bottom);
        Assert.Equal(Unit.Parse("4pt"), margins.Left);
    }

    [Fact]
    public void MarginsDoNotExposePublicChangeTracking()
    {
        Assert.Null(typeof(Margins).GetProperty("IsModified"));
        Assert.Null(typeof(Margins).GetMethod("AcceptChanges"));
    }

    [Fact]
    public void PageSizeEqualityUsesWidthAndHeight()
    {
        var first = new PageSize("300pt", "500pt");
        var equal = new PageSize(Unit.FromPoint(300), Unit.FromPoint(500));
        var different = new PageSize("500pt", "300pt");

        Assert.True(first == equal);
        Assert.False(first != equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void PortraitRenderingKeepsPageSizeWidthAndHeight()
    {
        var document = PageDocument(PageOrientation.Portrait);

        var page = new DocumentRenderer().Render(document).Pages.Single();

        Assert.Equal(Unit.Parse("300pt"), page.Width);
        Assert.Equal(Unit.Parse("500pt"), page.Height);
    }

    [Fact]
    public void LandscapeRenderingSwapsPageSizeWidthAndHeight()
    {
        var document = PageDocument(PageOrientation.Landscape);

        var page = new DocumentRenderer().Render(document).Pages.Single();

        Assert.Equal(Unit.Parse("500pt"), page.Width);
        Assert.Equal(Unit.Parse("300pt"), page.Height);
    }

    [Fact]
    public void MatrixIdentityLeavesPointsUnchanged()
    {
        var point = Matrix.Identity.Transform(3, 4);

        Assert.Equal((3d, 4d), point);
        Assert.Equal(1, Matrix.Identity.A);
        Assert.Equal(0, Matrix.Identity.B);
        Assert.Equal(0, Matrix.Identity.C);
        Assert.Equal(1, Matrix.Identity.D);
        Assert.Equal(0, Matrix.Identity.E);
        Assert.Equal(0, Matrix.Identity.F);
    }

    [Fact]
    public void MatrixFactoriesTransformPoints()
    {
        Assert.Equal((13d, 24d), Matrix.Translate(10, 20).Transform(3, 4));
        Assert.Equal((6d, 12d), Matrix.Scale(2, 3).Transform(3, 4));

        var rotated = Matrix.Rotate(90).Transform(10, 0);
        Assert.Equal(0, rotated.X, 9);
        Assert.Equal(10, rotated.Y, 9);
    }

    [Fact]
    public void MatrixMultiplicationAppliesLeftThenRight()
    {
        var operatorResult = (Matrix.Translate(10, 0) * Matrix.Rotate(90)).Transform(0, 0);
        var methodResult = Matrix.Multiply(Matrix.Translate(10, 0), Matrix.Rotate(90)).Transform(0, 0);

        Assert.Equal(0, operatorResult.X, 9);
        Assert.Equal(10, operatorResult.Y, 9);
        Assert.Equal(operatorResult, methodResult);
    }

    [Fact]
    public void MatrixEqualityUsesAllSixComponents()
    {
        var first = Matrix.Translate(4, 6);
        var equal = Matrix.Translate(1, 2) * Matrix.Translate(3, 4);
        var different = Matrix.Scale(4, 6);

        Assert.True(first == equal);
        Assert.False(first != equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(first, different);
    }

    private static Document PageDocument(PageOrientation orientation)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize("300pt", "500pt");
        section.Orientation = orientation;
        section.Blocks.AddParagraph("page");
        return document;
    }
}
