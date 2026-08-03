using Radzen.Documents.Pdf.Geometry;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class BottomUpSpaceTests
{
    [Fact]
    public void NestedRowBottom_SubtractsRowBottomOffsetFromNestedTop()
    {
        var nestedTop = BottomUpSpace.FromTop(top: 700, y: 40);

        var rowBottom = BottomUpSpace.FromTop(nestedTop, 10 + 20);

        Assert.Equal(630, rowBottom);
    }
}
