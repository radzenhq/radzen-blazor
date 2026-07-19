#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentBuilderTests
{
    [Fact]
    public void Section_MarginConvenienceSetsAllEdges()
    {
        var s = new DocumentBuilder().Sections.Add();
        s.Margin = Unit.FromPoint(18);
        Assert.Equal(18, s.Margins.Top.Point, 9);
        Assert.Equal(18, s.Margins.Right.Point, 9);
        Assert.Equal(18, s.Margins.Bottom.Point, 9);
        Assert.Equal(18, s.Margins.Left.Point, 9);
    }
}
